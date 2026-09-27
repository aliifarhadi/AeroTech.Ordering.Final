using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder;
using AeroTech.Ordering.Application.OrderAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.ElectronicTicketAggregate;
using AeroTech.Ordering.Persistence.FulfillmentReservationAggregate;
using AeroTech.Ordering.Persistence.FulfillmentTaskAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ScopeSafeCancellationRecoveryPersistenceTests : IAsyncLifetime
{
    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly InMemoryDistributedLock _lock = new();
    private readonly SequentialRecordLocatorGenerator _recordLocators = new();
    private readonly RecordingQueryDbSynchronizer _synchronizer = new();
    private readonly ScriptedFlightFlowProvider _flightFlow;
    private readonly StubAirFareReservationValidator _airFareValidator;
    private readonly TestDatabase _database;

    public ScopeSafeCancellationRecoveryPersistenceTests()
    {
        _database = new TestDatabase(_clock);
        _flightFlow = new ScriptedFlightFlowProvider(OrderFixture.FlightIdOfCapacity, new InMemoryUnitOfWork());
        _airFareValidator = new StubAirFareReservationValidator(_clock);
    }

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Persisted_unresolved_effect_governs_recovery_after_a_restart()
    {
        var order = OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101).Id;
        var inbound = ReservationHarness.AirService(order, 1, 201).Id;

        await InContextAsync(async context => await new OrderRepository(context).AddAsync(order));
        await InContextAsync(context => Services(context).Reserve.ReserveOrderAsync(order.Id, UnrestrictedOrderAuthorization.Instance));
        await InContextAsync(context => Services(context).Confirm.ConfirmReservedCapacityAsync(order.Id, UnrestrictedOrderAuthorization.Instance));
        _flightFlow.CancelConfirmedResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The confirmed-seat cancellation timed out."));
        await InContextAsync(context => CancelAsync(context, order.Id, outbound));

        var blocked = await Assert.ThrowsAsync<BusinessException>(() => InContextAsync(context => CancelAsync(context, order.Id, inbound)));
        var afterBlock = await SnapshotAsync(order.Id, outbound, inbound);

        await InContextAsync(context => CancelAsync(context, order.Id, outbound));
        var afterRetry = await SnapshotAsync(order.Id, outbound, inbound);

        Assert.Equal(2778, blocked.Code);
        Assert.Equal(
            (1, OrderFulfillmentStatus.Unknown, 1, ReservationMemberStatus.Unknown, ReservationMemberStatus.Confirmed, OrderServiceCommercialState.Active, OrderServiceCommercialState.Active, 1),
            (afterBlock.CancelRequests, afterBlock.TaskStatus, afterBlock.TaskCount, afterBlock.OutboundUnit, afterBlock.InboundUnit, afterBlock.Outbound, afterBlock.Inbound, afterBlock.Version));
        Assert.Equal(
            (2, OrderFulfillmentStatus.Succeeded, 1, ReservationMemberStatus.Cancelled, ReservationMemberStatus.Confirmed, OrderServiceCommercialState.Cancelled, OrderServiceCommercialState.Active, 2),
            (afterRetry.CancelRequests, afterRetry.TaskStatus, afterRetry.TaskCount, afterRetry.OutboundUnit, afterRetry.InboundUnit, afterRetry.Outbound, afterRetry.Inbound, afterRetry.Version));
        Assert.Equal(_flightFlow.CancelConfirmedRequests[0].SeatHoldReferences, _flightFlow.CancelConfirmedRequests[1].SeatHoldReferences);
    }

    private Task<CancelOrderResult> CancelAsync(OrderingDbContext context, long orderId, long serviceId)
        => Services(context).Cancel.CancelAsync(orderId, [serviceId], VoidReason.CustomerRequest, null, UnrestrictedOrderAuthorization.Instance, ReservationHarness.CancellingActor);

    private async Task InContextAsync(Func<OrderingDbContext, Task> operate)
    {
        await using var context = _database.NewContext();
        await operate(context);
        await context.SaveChangesAsync();
    }

    private async Task<Snapshot> SnapshotAsync(long orderId, long outbound, long inbound)
    {
        await using var context = _database.NewContext();
        var order = (await new OrderRepository(context).GetAsync(orderId))!;
        var reservation = (await new FulfillmentReservationRepository(context).ListByOrderAsync(orderId)).Single();
        var tasks = await context.FulfillmentTasks.Where(task => task.TaskType == OrderFulfillmentTaskType.CancelConfirmed).ToListAsync();

        return new Snapshot(
            _flightFlow.CancelConfirmedRequests.Count,
            tasks.Single().Status,
            tasks.Count,
            reservation.Units.Single(unit => unit.OrderServiceIds.Contains(outbound)).Status,
            reservation.Units.Single(unit => unit.OrderServiceIds.Contains(inbound)).Status,
            order.Services.Single(service => service.Id == outbound).CommercialStatus,
            order.Services.Single(service => service.Id == inbound).CommercialStatus,
            order.CommercialVersion);
    }

    private ServiceSet Services(OrderingDbContext context)
    {
        var orders = new OrderRepository(context);
        var reservations = new FulfillmentReservationRepository(context);
        var tasks = new FulfillmentTaskRepository(context);
        var providers = new ReservationProviderResolver([new FlightFlowReservationProvider(_flightFlow, _airFareValidator)]);
        var summarizer = new OrderReservationSummarizer(
            providers,
            new RecordLocatorAllocator(_recordLocators, orders, Options.Create(new RecordLocatorOptions { MaxAllocationAttempts = 3 })));
        var reservationLock = new ReservationLock(_lock, Options.Create(new FulfillmentOptions { LockExpirySeconds = 30 }));

        return new ServiceSet(
            new ReserveService(orders, reservations, tasks, providers, summarizer, reservationLock, _synchronizer, context, _ids, _clock),
            new ConfirmService(orders, reservations, tasks, providers, summarizer, reservationLock, _synchronizer, context, _ids, _clock),
            new CancelOrderService(
                orders,
                reservations,
                tasks,
                new ElectronicTicketRepository(context),
                new CancellationPlanner(),
                new ReservationReleaser(tasks, providers, context, _ids, _clock),
                new ConfirmedCapacityCanceller(tasks, providers, context, _ids, _clock),
                summarizer,
                reservationLock,
                _synchronizer,
                context,
                _ids,
                _clock));
    }

    private sealed record ServiceSet(ReserveService Reserve, ConfirmService Confirm, CancelOrderService Cancel);

    private sealed record Snapshot(
        int CancelRequests,
        OrderFulfillmentStatus TaskStatus,
        int TaskCount,
        ReservationMemberStatus OutboundUnit,
        ReservationMemberStatus InboundUnit,
        OrderServiceCommercialState Outbound,
        OrderServiceCommercialState Inbound,
        int Version);
}
