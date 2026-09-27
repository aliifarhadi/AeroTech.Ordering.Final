using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock;
using AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock.Backoffice;
using AeroTech.Ordering.Application.DocumentStockAggregate.Services;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder;
using AeroTech.Ordering.Application.OrderAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation;
using AeroTech.Ordering.Application.OrderAggregate.Services.Issuance;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public sealed class ReservationHarness
{
    private readonly IReserveService _reserve;
    private readonly IReleaseReservationService _release;
    private readonly IConfirmService _confirm;
    private readonly IReservationDeadlineService _deadlines;
    private readonly IIssueOrderService _issue;
    private readonly IDefineDocumentStockService _stocks;
    private readonly ICancelOrderService _cancel;
    private readonly IVoidElectronicTicketsService _void;

    public const long IssueActorId = 77;

    public const long IssuingOfficeId = 5;

    public static readonly SalesContext CancellingActor =
        new(SalesChannel.BackOffice, CallerContextType.Airline, CallerPrincipalType.Human, 88, 88, null, null, null, null, SellingOfficeKind.AirlineOffice, IssuingOfficeId);

    public ReservationHarness(params IReservationProvider[] additionalProviders)
    {
        UnitOfWork = new InMemoryUnitOfWork();
        Tasks = new InMemoryFulfillmentTaskRepository(UnitOfWork);
        Reservations = new InMemoryFulfillmentReservationRepository(UnitOfWork, Orders, Tasks);
        Tickets = new InMemoryElectronicTicketRepository(UnitOfWork);
        Stocks = new InMemoryDocumentStockRepository(UnitOfWork);
        FlightFlow = new ScriptedFlightFlowProvider(OrderFixture.FlightIdOfCapacity, UnitOfWork);
        AirFareValidator = new StubAirFareReservationValidator(Clock);

        FlightFlowReservation = new FlightFlowReservationProvider(FlightFlow, AirFareValidator);

        var providers = new ReservationProviderResolver([FlightFlowReservation, .. additionalProviders]);
        var recordLocators = new RecordLocatorAllocator(RecordLocators, Orders, Options.Create(new RecordLocatorOptions { MaxAllocationAttempts = 3 }));
        var summarizer = new OrderReservationSummarizer(providers, recordLocators);
        var reservationLock = new ReservationLock(Lock, Options.Create(new FulfillmentOptions { LockExpirySeconds = 30 }));

        var releaser = new ReservationReleaser(Tasks, providers, UnitOfWork, Ids, Clock);

        _reserve = new ReserveService(Orders, Reservations, Tasks, providers, summarizer, reservationLock, Synchronizer, UnitOfWork, Ids, Clock);
        _release = new ReleaseReservationService(Orders, Reservations, releaser, summarizer, reservationLock, Synchronizer, UnitOfWork, Clock);
        _confirm = new ConfirmService(Orders, Reservations, Tasks, providers, summarizer, reservationLock, Synchronizer, UnitOfWork, Ids, Clock);
        _deadlines = new ReservationDeadlineService(Orders, Reservations, providers, releaser, summarizer, reservationLock, Synchronizer, UnitOfWork, Clock);

        var stockLock = new DocumentStockLock(Lock, Options.Create(new FulfillmentOptions { LockExpirySeconds = 30 }));

        _stocks = new DefineDocumentStockService(Stocks, stockLock, Synchronizer, UnitOfWork, Ids);
        _issue = new IssueOrderService(
            Orders,
            Reservations,
            Tasks,
            Tickets,
            Stocks,
            providers,
            new IssuancePlanner(providers),
            reservationLock,
            stockLock,
            Synchronizer,
            Synchronizer,
            Synchronizer,
            OfficeTimeZones,
            UnitOfWork,
            Ids,
            Clock);
        _cancel = new CancelOrderService(
            Orders,
            Reservations,
            Tasks,
            Tickets,
            new CancellationPlanner(),
            releaser,
            new ConfirmedCapacityCanceller(Tasks, providers, UnitOfWork, Ids, Clock),
            summarizer,
            reservationLock,
            Synchronizer,
            UnitOfWork,
            Ids,
            Clock);
        _void = new VoidElectronicTicketsService(Orders, Reservations, Tasks, Tickets, summarizer, reservationLock, Synchronizer, Synchronizer, UnitOfWork, Ids, Clock);
    }

    public FixedClock Clock { get; } = new();

    public SequentialIdGenerator Ids { get; } = new();

    public InMemoryUnitOfWork UnitOfWork { get; }

    public InMemoryOrderRepository Orders { get; } = new();

    public InMemoryFulfillmentReservationRepository Reservations { get; }

    public InMemoryFulfillmentTaskRepository Tasks { get; }

    public InMemoryElectronicTicketRepository Tickets { get; }

    public InMemoryDocumentStockRepository Stocks { get; }

    public InMemoryDistributedLock Lock { get; } = new();

    public RecordingQueryDbSynchronizer Synchronizer { get; } = new();

    public SequentialRecordLocatorGenerator RecordLocators { get; } = new();

    public ScriptedFlightFlowProvider FlightFlow { get; }

    public FlightFlowReservationProvider FlightFlowReservation { get; }

    public StubAirFareReservationValidator AirFareValidator { get; }

    public StubAirlineOfficeTimeZoneResolver OfficeTimeZones { get; } = new();

    public Order SeedOrder(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<SeatSpec>? seats = null,
        DateTimeOffset? lastTicketingDate = null,
        IReadOnlyList<PricingUnitSpec>? pricingUnits = null)
    {
        var order = OrderFixture.Create(Ids, Clock, travellers, bounds, seats, lastTicketingDate, pricingUnits);
        Orders.Seed(order);
        return order;
    }

    public Order SeedOrder(OfferDetail offer, IReadOnlyList<TravellerSpec> travellers)
    {
        var order = OrderFixture.CreateFrom(offer, Ids, Clock, travellers);
        Orders.Seed(order);
        return order;
    }

    public Task<DocumentStockResult> DefineStockAsync(
        long rangeFrom = 1,
        long rangeTo = 999,
        string prefix = "0991",
        AccountableDocumentKind documentKind = AccountableDocumentKind.ElectronicTicket,
        string checkDigitProfile = "None",
        int serialWidth = 10,
        int ownerAirlineId = 10,
        long? officeId = 5)
        => _stocks.DefineAsync(new BackofficeDefineDocumentStockCommand(
            ownerAirlineId,
            officeId,
            documentKind,
            prefix,
            serialWidth,
            checkDigitProfile,
            rangeFrom,
            rangeTo));

    public Task<IssueOrderResult> IssueAsync(Order order, long ticketDocumentStockId)
        => _issue.IssueAsync(order.Id, ticketDocumentStockId, UnrestrictedOrderAuthorization.Instance, IssueActorId);

    public Task<CancelOrderResult> CancelAsync(Order order, params long[] serviceIds)
        => _cancel.CancelAsync(order.Id, serviceIds, VoidReason.CustomerRequest, null, UnrestrictedOrderAuthorization.Instance, CancellingActor);

    public Task<CancelOrderResult> CancelThroughBackofficeAsync(Order order, string? reasonDetail, params long[] serviceIds)
        => new BackofficeCancelOrderCommandHandler(_cancel, new FixedSalesContextFactory(CancellingActor))
            .Handle(new BackofficeCancelOrderCommand(order.Id, serviceIds, VoidReason.CustomerRequest, reasonDetail), CancellationToken.None);

    public Task<VoidElectronicTicketsResult> VoidAsync(Order order, long? callerOfficeId, params ElectronicTicket[] tickets)
        => VoidTargetsAsync(order, callerOfficeId, tickets.Select(ticket => new ElectronicTicketVoidTarget(ticket.Id, ticket.DocumentVersion)).ToList());

    public Task<VoidElectronicTicketsResult> VoidTargetsAsync(Order order, long? callerOfficeId, IReadOnlyList<ElectronicTicketVoidTarget> targets)
        => _void.VoidAsync(
            order.Id,
            targets,
            VoidReason.CustomerRequest,
            "  Passenger changed plans  ",
            UnrestrictedOrderAuthorization.Instance,
            IssueActorId,
            callerOfficeId);

    public Task<ReserveResult> ReserveOrderAsync(Order order)
        => _reserve.ReserveOrderAsync(order.Id, UnrestrictedOrderAuthorization.Instance);

    public Task<ReserveResult> ReserveServicesAsync(Order order, params long[] orderServiceIds)
        => _reserve.ReserveServicesAsync(order.Id, orderServiceIds, UnrestrictedOrderAuthorization.Instance);

    public Task<ReleaseReservationResult> ReleaseAsync(Order order, long reservationId)
        => _release.ReleaseAsync(order.Id, reservationId, UnrestrictedOrderAuthorization.Instance);

    public Task<ConfirmResult> ConfirmReservedCapacityAsync(Order order)
        => _confirm.ConfirmReservedCapacityAsync(order.Id, UnrestrictedOrderAuthorization.Instance);

    public Task<ConfirmResult> ConfirmReservationsAsync(Order order, params long[] reservationIds)
        => _confirm.ConfirmReservationsAsync(order.Id, reservationIds, UnrestrictedOrderAuthorization.Instance);

    public Task<IReadOnlyList<long>> DueOrderIdsAsync(int batchSize = 100)
        => _deadlines.ListDueOrderIdsAsync(batchSize);

    public Task EnforceDeadlinesAsync(Order order)
        => _deadlines.EnforceAsync(order.Id);

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
