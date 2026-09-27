using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Entities;
using AeroTech.Ordering.Persistence.DocumentStockAggregate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class DocumentStockRepositoryTests : IAsyncLifetime
{
    private const long TaskId = 9;
    private const string Role = "ETKT:11:1";

    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public DocumentStockRepositoryTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Stock_is_loaded_with_its_persisted_allocations()
    {
        var stock = await PersistedStockAsync(StockNumberState.Reserved);

        await using var context = _database.NewContext();
        var loaded = (await new DocumentStockRepository(context).GetAsync(stock.Id))!;

        Assert.Equal(stock.Allocations.Select(Evidence), loaded.Allocations.Select(Evidence));
    }

    [Theory]
    [InlineData(StockNumberState.Reserved)]
    [InlineData(StockNumberState.Issued)]
    public async Task Persisted_allocation_is_replayed_for_the_same_task_and_role(StockNumberState state)
    {
        var stock = await PersistedStockAsync(state);
        var persisted = stock.Allocations.Single();

        await using var context = _database.NewContext();
        var loaded = (await new DocumentStockRepository(context).GetAsync(stock.Id))!;
        var replayed = loaded.Allocate(TaskId, Role, _ids, _clock.Now);
        await context.SaveChangesAsync();

        Assert.Equal((persisted.Id, persisted.Serial, persisted.DocumentNumber, state), (replayed.Id, replayed.Serial, replayed.DocumentNumber, replayed.State));
        Assert.Equal((2L, 1), await PersistedCursorAsync(stock.Id));
    }

    [Fact]
    public async Task Persisted_retired_allocation_is_never_numbered_again()
    {
        var stock = await PersistedStockAsync(StockNumberState.Retired);

        await using var context = _database.NewContext();
        var loaded = (await new DocumentStockRepository(context).GetAsync(stock.Id))!;
        var exception = Assert.Throws<BusinessException>(() => loaded.Allocate(TaskId, Role, _ids, _clock.Now));
        await context.SaveChangesAsync();

        Assert.Equal((2802, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal((2L, 1), (loaded.NextNumber, loaded.Allocations.Count));
        Assert.Equal((2L, 1), await PersistedCursorAsync(stock.Id));
    }

    [Fact]
    public async Task Reload_under_the_stock_lock_sees_allocations_persisted_elsewhere()
    {
        var stock = await PersistedStockAsync(StockNumberState.Reserved);
        var first = stock.Allocations.Single();

        await using var locked = _database.NewContext();
        var repository = new DocumentStockRepository(locked);
        var view = (await repository.GetAsync(stock.Id))!;

        await using (var elsewhere = _database.NewContext())
        {
            var current = (await new DocumentStockRepository(elsewhere).GetAsync(stock.Id))!;
            current.MarkIssued(first.Id, _clock.Now);
            current.Allocate(TaskId, "ETKT:12:1", _ids, _clock.Now);
            await elsewhere.SaveChangesAsync();
        }

        await repository.ReloadAsync(view);
        var reloaded = (view.NextNumber, view.Allocations.Count, view.Allocations.Single(allocation => allocation.Id == first.Id).State);
        var second = view.Allocations.Single(allocation => allocation.DocumentRole == "ETKT:12:1");
        var replayed = view.Allocate(TaskId, "ETKT:12:1", _ids, _clock.Now);

        Assert.Equal((3L, 2, StockNumberState.Issued), reloaded);
        Assert.Same(second, replayed);
        Assert.Equal((3L, 2), (view.NextNumber, view.Allocations.Count));
    }

    [Fact]
    public async Task Logical_allocation_identity_is_unique_even_after_retirement()
    {
        var stock = await PersistedStockAsync(StockNumberState.Retired);

        await using var context = _database.NewContext();
        var exception = await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlAsync($"""
            INSERT INTO [Order].[DocumentStockAllocations]
                ([Id], [DocumentStockId], [IssueFulfillmentTaskId], [DocumentRole], [Serial], [DocumentNumber], [State], [AllocatedAt], [LastUpdateTime])
            VALUES
                ({_ids.NewId()}, {stock.Id}, {TaskId}, {Role}, 2, N'09910000000002', {(int)StockNumberState.Reserved}, {_clock.Now}, {_clock.Now});
            """));

        Assert.Equal(2601, exception.Number);
        Assert.Equal((2L, 1), await PersistedCursorAsync(stock.Id));
    }

    private async Task<DocumentStock> PersistedStockAsync(StockNumberState state)
    {
        var stock = DocumentStock.Define(_ids.NewId(), 10, 5, AccountableDocumentKind.ElectronicTicket, "0991", 10, DocumentStock.NoCheckDigitProfile, 1, 999);
        var allocation = stock.Allocate(TaskId, Role, _ids, _clock.Now);

        if (state == StockNumberState.Issued)
            stock.MarkIssued(allocation.Id, _clock.Now);

        if (state == StockNumberState.Retired)
            stock.Retire(allocation.Id, _clock.Now);

        await using var context = _database.NewContext();
        await new DocumentStockRepository(context).AddAsync(stock);
        await context.SaveChangesAsync();

        return stock;
    }

    private async Task<(long NextNumber, int Allocations)> PersistedCursorAsync(long stockId)
    {
        await using var context = _database.NewContext();
        var stock = await context.DocumentStocks.Include(item => item.Allocations).SingleAsync(item => item.Id == stockId);

        return (stock.NextNumber, stock.Allocations.Count);
    }

    private static object Evidence(DocumentStockAllocation allocation)
        => (allocation.Id, allocation.DocumentStockId, allocation.IssueFulfillmentTaskId, allocation.DocumentRole, allocation.Serial, allocation.DocumentNumber, allocation.State,
            allocation.AllocatedAt, allocation.SettledAt);
}
