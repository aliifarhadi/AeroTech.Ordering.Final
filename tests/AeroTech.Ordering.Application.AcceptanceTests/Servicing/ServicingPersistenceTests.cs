using System.Text.Json;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application._Shared.Events;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.ElectronicTicketAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.TicketFixture;
using VoidedV1 = AeroTech.Messages.Ordering.IntegrationEvents.V1.ElectronicTicketVoided;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ServicingPersistenceTests : IAsyncLifetime
{
    private const long VoidTaskId = 900;
    private const long IssuingOfficeId = 5;

    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public ServicingPersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Voided_ticket_keeps_its_void_record_deadline_and_coupon_truth_after_reload()
    {
        var ticket = await PersistedTicketAsync();
        var record = Record();

        await SaveAsync(async context => (await TicketOf(context)).Void(record, VoidReason.CustomerRequest, ReservationHarness.IssueActorId, IssuingOfficeId, _ids));

        await using var reader = _database.NewContext();
        var reloaded = await TicketOf(reader);
        Assert.Equal(record, reloaded.VoidRecord);
        Assert.Equal((ElectronicTicketStatus.Voided, 2, LocalMidnightAfterIssue), (reloaded.StatusSummary, reloaded.DocumentVersion, reloaded.VoidDeadline!.Value));
        Assert.All(reloaded.Coupons, coupon => Assert.Equal(TicketCouponFinancialStatus.Void, coupon.FinancialStatus));
        Assert.Equal((ticket.DocumentNumber, ticket.IssuedTotal), (reloaded.DocumentNumber, reloaded.IssuedTotal));
    }

    [Fact]
    public async Task Stale_void_writer_is_rejected_and_only_one_void_reaches_the_outbox()
    {
        await PersistedTicketAsync();
        await using var services = _database.NewOutboxServices();
        await using var first = services.CreateAsyncScope();
        await using var second = services.CreateAsyncScope();
        var firstContext = first.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var secondContext = second.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var firstView = await TicketOf(firstContext);
        var secondView = await TicketOf(secondContext);

        firstView.Void(Record(), VoidReason.CustomerRequest, ReservationHarness.IssueActorId, IssuingOfficeId, _ids);
        secondView.Void(Record(), VoidReason.Duplicate, ReservationHarness.IssueActorId, IssuingOfficeId, _ids);
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

        await using var reader = _database.NewContext();
        Assert.Equal(nameof(VoidReason.CustomerRequest), (await TicketOf(reader)).VoidRecord!.ReasonCode);
        Assert.Single(await reader.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Voided_event_names_the_void_task_as_its_operation()
    {
        var ticket = await PersistedTicketAsync();
        await using var services = _database.NewOutboxServices();

        await SaveInScopeAsync(services, async context => (await TicketOf(context)).Void(Record(), VoidReason.CustomerRequest, ReservationHarness.IssueActorId, IssuingOfficeId, _ids));

        await using var reader = _database.NewContext();
        var message = Assert.Single(await reader.OutboxMessages.ToListAsync());
        var published = JsonSerializer.Deserialize<VoidedV1>(message.Payload)!;
        Assert.Equal($"{typeof(VoidedV1).FullName}, {typeof(VoidedV1).Assembly.GetName().Name}", message.MessageType);
        Assert.Equal(
            (ticket.Id, ticket.Id.ToString(), OrderId, ticket.DocumentNumber, VoidTaskId, VoidReason.CustomerRequest, "Passenger changed plans"),
            (published.ElectronicTicketId, published.AggregateId, published.OrderId, published.DocumentNumber, published.OperationId, published.Reason, published.ReasonDetail));
        Assert.Equal((ReservationHarness.IssueActorId, _clock.Now, (string?)null, 2), (published.VoidedBy, published.VoidedAt, published.ProviderReference, published.DocumentVersion));
    }

    [Fact]
    public async Task Cancellation_history_survives_persistence_and_publishes_no_order_cancelled_event()
    {
        var order = OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        var firstLeg = ReservationHarness.AirService(order, 1, 101);
        await SaveAsync(context => new OrderRepository(context).AddAsync(order));
        await using var services = _database.NewOutboxServices();
        long changeId = 0;

        await SaveInScopeAsync(services, async context =>
        {
            var current = (await new OrderRepository(context).GetAsync(order.Id))!;
            changeId = current.Cancel([firstLeg.Id], ReservationHarness.CancellingActor, nameof(VoidReason.CustomerRequest), _ids, _clock.Now).Id;
        });

        await using var reader = _database.NewContext();
        var reloaded = (await new OrderRepository(reader).GetAsync(order.Id))!;
        var change = reloaded.Changes.Single(item => item.Id == changeId);
        Assert.Equal(
            (OrderChangeType.Cancel, 2, 2, (string?)null, nameof(VoidReason.CustomerRequest), false, (string?)null, (string?)null),
            (change.ChangeType, change.CommercialVersion, reloaded.CommercialVersion, change.SourceSystem, change.ReasonCode, change.IsInvoluntary, change.WaiverCode, change.SourceReference));
        Assert.Equal(ReservationHarness.CancellingActor, change.ActorContext);
        Assert.Equal(
            (OrderServiceCommercialState.Cancelled, (long?)changeId, OrderServiceCommercialState.Active, (long?)null),
            (reloaded.Services.Single(service => service.Id == firstLeg.Id).CommercialStatus, reloaded.Services.Single(service => service.Id == firstLeg.Id).EndedByChangeId,
             ReservationHarness.AirService(reloaded, 1, 102).CommercialStatus, ReservationHarness.AirService(reloaded, 1, 102).EndedByChangeId));
        Assert.Equal((OrderItemCommercialState.Active, (long?)null), (reloaded.Items.Single().CommercialStatus, reloaded.Items.Single().EndedByChangeId));
        Assert.Empty(await reader.OutboxMessages.ToListAsync());
    }

    [Fact]
    public void Ordering_has_no_order_cancelled_publisher()
        => Assert.DoesNotContain(
            typeof(MediatRDomainEventDispatcher).Assembly.GetTypes(),
            type => type.GetInterfaces().Any(contract => contract.IsGenericType
                                                         && contract.GetGenericTypeDefinition() == typeof(INotificationHandler<>)
                                                         && contract.GenericTypeArguments[0].GenericTypeArguments.Any(argument => argument.Name == "OrderCancelled")));

    private async Task<ElectronicTicket> PersistedTicketAsync()
    {
        var ticket = Issue(_ids, Args([Coupon(101), Coupon(202)]) with { VoidDeadline = LocalMidnightAfterIssue });

        await SaveAsync(context => new ElectronicTicketRepository(context).AddAsync(ticket));

        return ticket;
    }

    private static async Task<ElectronicTicket> TicketOf(OrderingDbContext context)
        => (await new ElectronicTicketRepository(context).ListByOrderAsync(OrderId)).Single();

    private DocumentVoidRecord Record()
        => new(VoidTaskId, nameof(VoidReason.CustomerRequest), "Passenger changed plans", null, ReservationHarness.IssueActorId, _clock.Now);

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
}
