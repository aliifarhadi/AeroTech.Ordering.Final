using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.ElectronicTicketAggregate;
using AeroTech.Ordering.Persistence.FulfillmentReservationAggregate;
using AeroTech.Ordering.Persistence.FulfillmentTaskAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById;
using AeroTech.Ordering.Query._Shared.Authorization;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class OrderChangeReasonPersistenceTests : IAsyncLifetime
{
    private const string ReasonDetail = "  customer changed plan  ";
    private const string ReasonText = "customer changed plan";

    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public OrderChangeReasonPersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var commands = _database.NewContext();
        await commands.Database.MigrateAsync();

        await using var queries = _database.NewQueryContext();
        await queries.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Backoffice_reason_detail_survives_a_new_command_context_as_reason_text()
    {
        var order = await CancelledThroughBackofficeAsync();

        await using var reader = _database.NewContext();
        var reloaded = (await new OrderRepository(reader).GetAsync(order.Id))!;

        Assert.Equal(
            [(OrderChangeType.Create, (string?)null, (string?)null), (OrderChangeType.Cancel, nameof(VoidReason.CustomerRequest), ReasonText)],
            reloaded.Changes.OrderBy(change => change.CommercialVersion).Select(change => (change.ChangeType, change.ReasonCode, change.ReasonText)));
    }

    [Fact]
    public async Task Backoffice_reason_detail_is_returned_by_the_order_detail_change_history()
    {
        var order = await CancelledThroughBackofficeAsync();

        await using var reader = _database.NewQueryContext();
        var detail = await new GetOrderByIdService(reader).ExecuteAsync(order.Id, OrderQueryScope.Unrestricted);

        Assert.Equal(
            [(OrderChangeType.Create, (string?)null, (string?)null), (OrderChangeType.Cancel, nameof(VoidReason.CustomerRequest), ReasonText)],
            detail!.Changes.Select(change => (change.ChangeType, change.ReasonCode, change.ReasonText)));
    }

    [Fact]
    public async Task Backoffice_order_detail_response_carries_the_reason_text()
    {
        var order = await CancelledThroughBackofficeAsync();

        await using var reader = _database.NewQueryContext();
        var detail = await new GetBackofficeOrderByIdService(new GetOrderByIdService(reader), new ReferenceCodeReader(reader)).ExecuteAsync(order.Id);

        Assert.Equal(
            [((string?)null, (string?)null), (nameof(VoidReason.CustomerRequest), ReasonText)],
            detail!.Changes.Select(change => (change.ReasonCode, change.ReasonText)));
    }

    private async Task<Order> CancelledThroughBackofficeAsync()
    {
        var order = OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);

        await using (var writer = _database.NewContext())
        {
            await new OrderRepository(writer).AddAsync(order);
            await writer.SaveChangesAsync();
        }

        await using var commands = _database.NewContext();
        await using var queries = _database.NewQueryContext();

        await new BackofficeCancelOrderCommandHandler(CancelOrderService(commands, queries), new FixedSalesContextFactory(ReservationHarness.CancellingActor))
            .Handle(
                new BackofficeCancelOrderCommand(order.Id, [ReservationHarness.AirService(order, 1, 101).Id], VoidReason.CustomerRequest, ReasonDetail),
                CancellationToken.None);

        return order;
    }

    private CancelOrderService CancelOrderService(OrderingDbContext commands, OrderQueryDbContext queries)
    {
        var orders = new OrderRepository(commands);
        var tasks = new FulfillmentTaskRepository(commands);
        var unitOfWork = new OrderingUnitOfWork(commands, queries);
        var providers = new ReservationProviderResolver(
        [
            new FlightFlowReservationProvider(
                new ScriptedFlightFlowProvider(OrderFixture.FlightIdOfCapacity, new InMemoryUnitOfWork()),
                new StubAirFareReservationValidator(_clock))
        ]);
        var summarizer = new OrderReservationSummarizer(
            providers,
            new RecordLocatorAllocator(new SequentialRecordLocatorGenerator(), orders, Options.Create(new RecordLocatorOptions { MaxAllocationAttempts = 3 })));

        return new CancelOrderService(
            orders,
            new FulfillmentReservationRepository(commands),
            tasks,
            new ElectronicTicketRepository(commands),
            new CancellationPlanner(),
            new ReservationReleaser(tasks, providers, unitOfWork, _ids, _clock),
            new ConfirmedCapacityCanceller(tasks, providers, unitOfWork, _ids, _clock),
            summarizer,
            new ReservationLock(new InMemoryDistributedLock(), Options.Create(new FulfillmentOptions { LockExpirySeconds = 30 })),
            new OrderQueryDbSynchronizer(queries),
            unitOfWork,
            _ids,
            _clock);
    }
}
