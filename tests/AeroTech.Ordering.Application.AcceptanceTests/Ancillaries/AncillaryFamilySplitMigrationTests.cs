using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Persistence.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class AncillaryFamilySplitMigrationTests : IAsyncLifetime
{
    private const string LastSingleTableAncillaryMigration = "20261028100934_AddAncillaryServices";

    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public AncillaryFamilySplitMigrationTests() => _database = new TestDatabase(_harness.Clock);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _database.DropAsync();

    [Fact]
    public async Task Every_persisted_family_survives_reverting_and_reapplying_the_family_table_split()
    {
        var order = await OrderWithEveryPersistedVariantAsync(_harness);

        await using (var context = _database.NewContext())
        {
            await context.Database.MigrateAsync();
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(LastSingleTableAncillaryMigration);

            var familyTables = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Order' AND TABLE_NAME IN ('OrderMealServices', 'OrderPetServices', 'OrderAssistedTravelServices', 'OrderAirportServices', 'OrderPriorityServices', 'OrderConnectivityServices', 'OrderUpgradeServices')")
                .SingleAsync();
            Assert.Equal(0, familyTables);

            await migrator.MigrateAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(PersistedPurchases(order), PersistedPurchases(reloaded));
        }
    }
}
