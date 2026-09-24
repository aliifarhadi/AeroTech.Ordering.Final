using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Services;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Providers.FlightFlow;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public sealed class ReservationHarness
{
    public const int HoldMinutes = 15;

    private readonly IReserveService _reserve;
    private readonly IReleaseReservationService _release;

    public ReservationHarness(params IReservationProvider[] additionalProviders)
    {
        UnitOfWork = new InMemoryUnitOfWork();
        Reservations = new InMemoryFulfillmentReservationRepository(UnitOfWork);
        Tasks = new InMemoryFulfillmentTaskRepository(UnitOfWork);
        FlightFlow = new ScriptedFlightFlowProvider(OrderFixture.FlightIdOfCapacity, UnitOfWork);
        AirFareValidator = new StubAirFareReservationValidator(Clock);

        FlightFlowReservation = new FlightFlowReservationProvider(
            FlightFlow,
            AirFareValidator,
            Clock,
            Options.Create(new FlightFlowReservationOptions { HoldMinutes = HoldMinutes }));

        var providers = new ReservationProviderResolver([FlightFlowReservation, .. additionalProviders]);
        var recordLocators = new RecordLocatorAllocator(RecordLocators, Orders, Options.Create(new RecordLocatorOptions { MaxAllocationAttempts = 3 }));
        var summarizer = new OrderReservationSummarizer(providers, recordLocators);
        var reservationLock = new ReservationLock(Lock, Options.Create(new FulfillmentOptions { LockExpirySeconds = 30 }));

        _reserve = new ReserveService(Orders, Reservations, Tasks, providers, summarizer, reservationLock, Synchronizer, UnitOfWork, Ids, Clock);
        _release = new ReleaseReservationService(Orders, Reservations, Tasks, providers, summarizer, reservationLock, Synchronizer, UnitOfWork, Ids, Clock);
    }

    public FixedClock Clock { get; } = new();

    public SequentialIdGenerator Ids { get; } = new();

    public InMemoryUnitOfWork UnitOfWork { get; }

    public InMemoryOrderRepository Orders { get; } = new();

    public InMemoryFulfillmentReservationRepository Reservations { get; }

    public InMemoryFulfillmentTaskRepository Tasks { get; }

    public InMemoryDistributedLock Lock { get; } = new();

    public RecordingQueryDbSynchronizer Synchronizer { get; } = new();

    public SequentialRecordLocatorGenerator RecordLocators { get; } = new();

    public ScriptedFlightFlowProvider FlightFlow { get; }

    public FlightFlowReservationProvider FlightFlowReservation { get; }

    public StubAirFareReservationValidator AirFareValidator { get; }

    public Order SeedOrder(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<SeatSpec>? seats = null,
        DateTimeOffset? lastTicketingDate = null,
        IReadOnlyList<FareSpec>? fares = null)
    {
        var order = OrderFixture.Create(Ids, Clock, travellers, bounds, seats, lastTicketingDate, fares);
        Orders.Seed(order);
        return order;
    }

    public Task<ReserveResult> ReserveOrderAsync(Order order)
        => _reserve.ReserveOrderAsync(order.Id, UnrestrictedOrderAuthorization.Instance);

    public Task<ReserveResult> ReserveServicesAsync(Order order, params long[] orderServiceIds)
        => _reserve.ReserveServicesAsync(order.Id, orderServiceIds, UnrestrictedOrderAuthorization.Instance);

    public Task<ReleaseReservationResult> ReleaseAsync(Order order, long reservationId)
        => _release.ReleaseAsync(order.Id, reservationId, UnrestrictedOrderAuthorization.Instance);

    public FulfillmentReservation Reservation(long reservationId)
        => Reservations.Committed.Single(reservation => reservation.Id == reservationId);

    public FulfillmentTask TaskOf(long reservationId, OrderFulfillmentTaskType taskType)
        => Tasks.Committed.Single(task => task.FulfillmentReservationId == reservationId && task.TaskType == taskType);

    public static IReadOnlyList<OrderAirTransportService> AirServices(Order order)
        => order.Services.OfType<OrderAirTransportService>().ToList();

    public static OrderAirTransportService AirService(Order order, int travellerIndex, long flightId)
    {
        var traveller = order.Travellers.Single(item => item.Index == travellerIndex);
        var segment = order.Segments.Single(item => item.FlightId == flightId);

        return AirServices(order).Single(service => service.TravellerId == traveller.Id && service.SegmentId == segment.Id);
    }

    public static void AssignProvider(Order order, int travellerIndex, string providerKey)
    {
        var traveller = order.Travellers.Single(item => item.Index == travellerIndex);

        foreach (var service in order.Services.Where(service => service.TravellerId == traveller.Id))
            typeof(OrderService).GetProperty(nameof(OrderService.FulfillmentProviderKey))!.SetValue(service, providerKey);
    }
}
