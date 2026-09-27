using System.Text.Json;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application._Shared.Events;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Entities;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.DocumentStockAggregate;
using AeroTech.Ordering.Persistence.ElectronicTicketAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.TicketFixture;
using IssuedV1 = AeroTech.Messages.Ordering.IntegrationEvents.V1.ElectronicTicketIssued;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class IssuancePersistenceTests : IAsyncLifetime
{
    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public IssuancePersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Ticket_coupons_and_price_links_survive_persistence_and_reload()
    {
        var ticket = Issue(_ids, Args([Coupon(101, 100m, 20m), Coupon(202, 80m)]));

        await SaveAsync(context => new ElectronicTicketRepository(context).AddAsync(ticket));

        await using var reader = _database.NewContext();
        var reloaded = Assert.Single(await new ElectronicTicketRepository(reader).ListByOrderAsync(OrderId));

        Assert.Equal(Evidence(ticket), Evidence(reloaded));
        Assert.Equal(ticket.Coupons.Select(Evidence), reloaded.Coupons.OrderBy(coupon => coupon.CouponNumber).Select(Evidence));
        Assert.Equal(ticket.PriceLinks.Select(Evidence), reloaded.PriceLinks.OrderBy(link => link.Id).Select(Evidence));
    }

    [Fact]
    public async Task Stock_and_allocations_survive_persistence_and_reload()
    {
        var stock = Stock("0991", 10);
        var issued = stock.Allocate(IssueFulfillmentTaskId, "ETKT:11:1", _ids, _clock.Now);
        stock.MarkIssued(issued.Id, _clock.Now);
        stock.Allocate(IssueFulfillmentTaskId, "ETKT:12:1", _ids, _clock.Now);

        await SaveAsync(context => new DocumentStockRepository(context).AddAsync(stock));

        await using var reader = _database.NewContext();
        var reloaded = await reader.DocumentStocks.Include(item => item.Allocations).SingleAsync();

        Assert.Equal(Evidence(stock), Evidence(reloaded));
        Assert.Equal(stock.Allocations.Select(Evidence), reloaded.Allocations.OrderBy(allocation => allocation.Serial).Select(Evidence));
    }

    [Fact]
    public async Task Document_number_is_unique_across_tickets()
    {
        await SaveAsync(context => new ElectronicTicketRepository(context).AddAsync(Issue(_ids, Args([Coupon(101)]))));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SaveAsync(context => new ElectronicTicketRepository(context).AddAsync(Issue(_ids, Args([Coupon(202)])))));

        await using var reader = _database.NewContext();
        Assert.True(await new ElectronicTicketRepository(reader).AnyWithDocumentNumberAsync([DocumentNumber]));
        Assert.Equal(1, await reader.ElectronicTickets.CountAsync());
    }

    [Fact]
    public async Task Document_number_is_unique_across_stock_allocations()
    {
        var first = Stock("0991", 10);
        var second = Stock("09910", 9);
        first.Allocate(1, "ETKT:1:1", _ids, _clock.Now);
        second.Allocate(2, "ETKT:1:1", _ids, _clock.Now);

        await SaveAsync(context => new DocumentStockRepository(context).AddAsync(first));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SaveAsync(context => new DocumentStockRepository(context).AddAsync(second)));

        await using var reader = _database.NewContext();
        Assert.Equal(DocumentNumber, (await reader.Set<DocumentStockAllocation>().SingleAsync()).DocumentNumber);
    }

    [Fact]
    public async Task Coupon_number_is_unique_within_a_ticket()
    {
        var ticket = Issue(_ids, Args([Coupon(101), Coupon(202)]));
        typeof(TicketCoupon).GetProperty(nameof(TicketCoupon.CouponNumber))!.SetValue(ticket.Coupons.Last(), 1);

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SaveAsync(context => new ElectronicTicketRepository(context).AddAsync(ticket)));

        await using var reader = _database.NewContext();
        Assert.Equal(0, await reader.ElectronicTickets.CountAsync());
    }

    [Fact]
    public async Task Stale_stock_writer_cannot_persist_a_second_copy_of_a_number()
    {
        var stock = Stock("0991", 10);
        await SaveAsync(context => new DocumentStockRepository(context).AddAsync(stock));

        await using var first = _database.NewContext();
        await using var second = _database.NewContext();
        var firstView = (await new DocumentStockRepository(first).GetAsync(stock.Id))!;
        var secondView = (await new DocumentStockRepository(second).GetAsync(stock.Id))!;

        firstView.Allocate(1, "ETKT:1:1", _ids, _clock.Now);
        secondView.Allocate(2, "ETKT:1:1", _ids, _clock.Now);
        await first.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => second.SaveChangesAsync());

        await using var reader = _database.NewContext();
        var persisted = await reader.DocumentStocks.Include(item => item.Allocations).SingleAsync();
        Assert.Equal((2L, 1L), (persisted.NextNumber, persisted.Allocations.Single().IssueFulfillmentTaskId));
        Assert.True(reader.Model.FindEntityType(typeof(DocumentStock))!.FindProperty(nameof(DocumentStock.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public async Task Issued_event_reaches_the_outbox_only_with_its_ticket()
    {
        await using var services = OutboxServices();

        await SaveInScopeAsync(services, context => new ElectronicTicketRepository(context).AddAsync(Issue(_ids, Args([Coupon(101)]))));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            SaveInScopeAsync(services, context => new ElectronicTicketRepository(context).AddAsync(Issue(_ids, Args([Coupon(202)])))));

        await using var reader = _database.NewContext();
        var message = Assert.Single(await reader.OutboxMessages.ToListAsync());
        Assert.Equal($"{typeof(IssuedV1).FullName}, {typeof(IssuedV1).Assembly.GetName().Name}", message.MessageType);
        Assert.Equal(1, await reader.ElectronicTickets.CountAsync());
    }

    [Fact]
    public async Task Issued_event_names_the_issue_task_as_its_operation()
    {
        var order = OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        await SaveAsync(context => new OrderRepository(context).AddAsync(order));
        await using var services = OutboxServices();
        ElectronicTicket ticket = null!;

        await SaveInScopeAsync(services, async context =>
        {
            var current = (await new OrderRepository(context).GetAsync(order.Id))!;
            var service = current.TicketableAirServices().Single();
            ticket = Issue(_ids, Args([Coupon(service.Id)]) with { OrderId = current.Id });
            current.MarkTicketed(new HashSet<long> { service.Id });
            await new ElectronicTicketRepository(context).AddAsync(ticket);
        });

        await using var reader = _database.NewContext();
        var published = JsonSerializer.Deserialize<IssuedV1>(Assert.Single(await reader.OutboxMessages.ToListAsync()).Payload)!;
        var coupon = ticket.Coupons.Single();

        Assert.Equal(
            (ticket.Id, ticket.Id.ToString(), order.Id, TravellerId, IssueFulfillmentTaskId, ticket.DocumentNumber, ticket.IssuedTotal),
            (published.ElectronicTicketId, published.AggregateId, published.OrderId, published.TravelerId, published.OperationId, published.DocumentNumber, published.IssuedTotal));
        Assert.Equal(
            [(coupon.Id, coupon.CouponNumber, coupon.CurrentOrderServiceId, coupon.OrderSegmentId, coupon.IssuanceValue)],
            published.Coupons.Select(item => (item.TicketCouponId, item.CouponNumber, item.OrderServiceId, item.JourneySegmentId, item.IssuanceValue)));
        Assert.Equal(OrderStatus.Ticketed, (await reader.Orders.SingleAsync()).Status);
    }

    [Fact]
    public void Ordering_publishes_no_order_issued_event()
        => Assert.DoesNotContain(
            typeof(MediatRDomainEventDispatcher).Assembly.GetTypes(),
            type => type.GetInterfaces().Any(contract => contract.IsGenericType
                                                         && contract.GetGenericTypeDefinition() == typeof(MediatR.INotificationHandler<>)
                                                         && contract.GenericTypeArguments[0].GenericTypeArguments.Any(argument => argument.Name == "OrderIssued")));

    private DocumentStock Stock(string prefix, int serialWidth)
        => DocumentStock.Define(_ids.NewId(), 10, 5, AccountableDocumentKind.ElectronicTicket, prefix, serialWidth, DocumentStock.NoCheckDigitProfile, 1, 999);

    private async Task SaveAsync(Func<OrderingDbContext, Task> stage)
    {
        await using var context = _database.NewContext();
        await stage(context);
        await context.SaveChangesAsync();
    }

    private static async Task SaveInScopeAsync(ServiceProvider services, Func<OrderingDbContext, Task> stage)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await stage(context);
        await context.SaveChangesAsync();
    }

    private ServiceProvider OutboxServices()
    {
        var services = new ServiceCollection();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<MediatRDomainEventDispatcher>());
        services.AddSingleton<IClock>(_clock);
        services.AddSingleton<IActorResolver, TestDatabase.AnonymousActorResolver>();
        services.AddSingleton(Options.Create(new IntegrationEventOptions()));
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped(provider => _database.NewContext(provider.GetRequiredService<IDomainEventDispatcher>()));
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services.BuildServiceProvider();
    }

    private static object Evidence(ElectronicTicket ticket)
        => (ticket.Id, ticket.OriginalOrderId, ticket.CurrentServicingOrderId, ticket.TravellerId, ticket.TravellerProfileRevisionId, ticket.IssueFulfillmentTaskId, ticket.DocumentNumber,
            ticket.IssuanceContext, ticket.Authority, ticket.IssuedAt, ticket.VoidDeadline, ticket.IssuedTotal, ticket.CurrencyId, ticket.ProviderReference,
            ticket.StatusSummary, ticket.DocumentVersion, ticket.PredecessorElectronicTicketId, ticket.PredecessorExchangeChangeId);

    private static object Evidence(TicketCoupon coupon)
        => (coupon.Id, coupon.TicketId, coupon.CouponNumber, coupon.OriginalOrderServiceId, coupon.CurrentOrderServiceId, coupon.OrderSegmentId, coupon.OrderFareComponentId,
            coupon.PredecessorTicketCouponId, coupon.IssuedSegment, coupon.FareBasisSnapshot, coupon.BookingClassSnapshot, coupon.RbdIdSnapshot, coupon.CabinClassIdSnapshot,
            coupon.BaggageAllowanceSnapshot, coupon.IssuanceValue, coupon.CurrencyId, coupon.FinancialStatus, coupon.ControlStatus, coupon.ProviderCouponStatusCode,
            coupon.NotValidBefore, coupon.NotValidAfter, coupon.UsedAt, coupon.UsageReference);

    private static object Evidence(TicketPriceLink link)
        => (link.Id, link.ElectronicTicketId, link.TicketCouponId, link.PricingLineId, link.PricingAllocationId, link.AttributedValue, link.CurrencyId);

    private static object Evidence(DocumentStock stock)
        => (stock.Id, stock.OwnerAirlineId, stock.OfficeId, stock.DocumentKind, stock.Prefix, stock.SerialWidth, stock.CheckDigitProfile, stock.RangeFrom, stock.RangeTo,
            stock.NextNumber, stock.Status);

    private static object Evidence(DocumentStockAllocation allocation)
        => (allocation.Id, allocation.DocumentStockId, allocation.IssueFulfillmentTaskId, allocation.DocumentRole, allocation.Serial, allocation.DocumentNumber, allocation.State,
            allocation.AllocatedAt, allocation.SettledAt);
}
