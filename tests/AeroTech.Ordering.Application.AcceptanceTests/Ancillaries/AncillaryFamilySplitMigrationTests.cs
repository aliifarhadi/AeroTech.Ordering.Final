using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.OrderAggregate;
using Microsoft.Data.SqlClient;
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
    public async Task An_order_written_by_the_single_table_model_upgrades_to_its_family_tables_unchanged()
    {
        var expected = await OrderWithEveryPersistedVariantAsync(_harness);

        await using (var context = _database.NewContext())
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(LastSingleTableAncillaryMigration);
            await context.Database.ExecuteSqlRawAsync(SingleTableOrderSeed());

            await migrator.MigrateAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(expected.Id))!;

            Assert.Equal(PersistedPurchases(expected), PersistedPurchases(reloaded));
            Assert.Equal(PersistedPricing(expected), PersistedPricing(reloaded));
        }
    }

    [Fact]
    public async Task A_single_table_row_without_its_family_data_stops_the_split_before_any_column_is_dropped()
    {
        await using var context = _database.NewContext();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(LastSingleTableAncillaryMigration);
        await context.Database.ExecuteSqlRawAsync(SingleTableOrderSeed());
        await context.Database.ExecuteSqlAsync($"UPDATE [Order].[OrderAncillaryServices] SET [MealKind] = NULL WHERE [VariantCode] = N'A11'");
        var rowsBefore = await AncillaryRowCountAsync(context);

        var refused = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());

        var singleTableColumns = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Order' AND TABLE_NAME = 'OrderAncillaryServices' AND COLUMN_NAME IN ('MealKind', 'PetTransportMode', 'UpgradeFromCabinId')")
            .SingleAsync();

        Assert.StartsWith("SplitAncillaryFamilies stopped before dropping any column: 1 ancillary service row(s)", refused.Message);
        Assert.Equal(LastSingleTableAncillaryMigration, (await context.Database.GetAppliedMigrationsAsync()).Last());
        Assert.Equal((3, 0, rowsBefore), (singleTableColumns, await FamilyTableCountAsync(context), await AncillaryRowCountAsync(context)));
    }

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

            Assert.Equal(0, await FamilyTableCountAsync(context));

            await migrator.MigrateAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(PersistedPurchases(order), PersistedPurchases(reloaded));
        }
    }

    private static string SingleTableOrderSeed()
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ancillaries", "Payloads", "single-table-ancillary-order.sql"));

    private static Task<int> FamilyTableCountAsync(OrderingDbContext context)
        => context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'Order' AND TABLE_NAME IN ('OrderMealServices', 'OrderPetServices', 'OrderAssistedTravelServices', 'OrderAirportServices', 'OrderPriorityServices', 'OrderConnectivityServices', 'OrderUpgradeServices')")
            .SingleAsync();

    private static Task<int> AncillaryRowCountAsync(OrderingDbContext context)
        => context.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [Order].[OrderAncillaryServices]")
            .SingleAsync();
}
