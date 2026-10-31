using System.Globalization;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Persistence;

[Collection(MigratedDatabaseCollection.Name)]
public sealed class OrderingUnitOfWorkTests : IAsyncLifetime
{
    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public OrderingUnitOfWorkTests(MigratedDatabaseFixture database) => _database = database.For(_harness.Clock);

    public Task InitializeAsync() => _database.ClearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task The_order_its_outbox_message_and_its_read_model_commit_together()
    {
        var (order, snapshot) = await NewOrderAsync();

        await using var services = _database.NewOutboxServices();
        await using var scope = services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using var query = _database.NewQueryContext();

        await StageAsync(command, query, order, snapshot);
        await new OrderingUnitOfWork(command, query).SaveChangesAsync();

        Assert.Equal((true, true, true), await StoredAsync(order));
    }

    [Fact]
    public async Task A_read_model_failure_rolls_the_order_and_its_outbox_message_back()
    {
        var (order, snapshot) = await NewOrderAsync();

        await using var services = _database.NewOutboxServices();
        await using var scope = services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using var query = _database.NewQueryContext();

        await StageAsync(command, query, order, snapshot);
        await ProjectFromAnotherConnectionAsync(snapshot);

        await Assert.ThrowsAsync<DbUpdateException>(() => new OrderingUnitOfWork(command, query).SaveChangesAsync());

        Assert.Equal((false, false, true), await StoredAsync(order));
    }

    [Fact]
    public async Task A_query_and_a_second_save_work_after_a_commit()
    {
        var (order, snapshot) = await NewOrderAsync();

        await using var services = _database.NewOutboxServices();
        await using var scope = services.CreateAsyncScope();
        var command = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using var query = _database.NewQueryContext();
        var unitOfWork = new OrderingUnitOfWork(command, query);

        await StageAsync(command, query, order, snapshot);
        await unitOfWork.SaveChangesAsync();

        var row = await query.Orders.SingleAsync(item => item.Id == order.Id);
        var projectedAgainAt = row.LastProjectedAt.AddMinutes(1);

        row.LastProjectedAt = projectedAgainAt;
        await unitOfWork.SaveChangesAsync();

        await using var reader = _database.NewQueryContext();

        Assert.Equal(projectedAgainAt, (await reader.Orders.SingleAsync(item => item.Id == order.Id)).LastProjectedAt);
    }

    private static async Task StageAsync(OrderingDbContext command, OrderQueryDbContext query, Order order, OrderReadModelSnapshot snapshot)
    {
        await new OrderRepository(command).AddAsync(order);
        await new OrderQueryDbSynchronizer(query).ProjectCreatedAsync(snapshot);
    }

    private async Task<(Order Order, OrderReadModelSnapshot Snapshot)> NewOrderAsync()
    {
        var order = await _harness.CreateFromOfferAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        return (order, Assert.Single(_harness.Synchronizer.OrderProjections));
    }

    private async Task ProjectFromAnotherConnectionAsync(OrderReadModelSnapshot snapshot)
    {
        await using var writer = _database.NewQueryContext();

        await new OrderQueryDbSynchronizer(writer).ProjectCreatedAsync(snapshot);
        await writer.SaveChangesAsync();
    }

    private async Task<(bool Order, bool Outbox, bool ReadModel)> StoredAsync(Order order)
    {
        var orderId = order.Id.ToString(CultureInfo.InvariantCulture);

        await using var command = _database.NewContext();
        await using var query = _database.NewQueryContext();

        return (
            await command.Orders.AnyAsync(row => row.Id == order.Id),
            await command.OutboxMessages.AnyAsync(message => message.Payload.Contains(orderId)),
            await query.Orders.AnyAsync(row => row.Id == order.Id));
    }
}
