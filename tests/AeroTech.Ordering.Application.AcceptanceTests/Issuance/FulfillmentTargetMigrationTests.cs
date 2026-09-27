using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Persistence.FulfillmentTaskAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class FulfillmentTargetMigrationTests : IAsyncLifetime
{
    private const string LastReservationOnlyMigration = "20260927075024_PreserveAirServiceCabinClassId";

    private readonly FixedClock _clock = new();
    private readonly TestDatabase _database;

    public FulfillmentTargetMigrationTests() => _database = new TestDatabase(_clock);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Legacy_reservation_unit_targets_become_typed_targets_with_the_same_identity()
    {
        await using (var context = _database.NewContext())
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(LastReservationOnlyMigration);

            await context.Database.ExecuteSqlAsync($"""
                INSERT INTO [Order].[FulfillmentTasks]
                    ([Id], [OrderId], [FulfillmentReservationId], [TaskType], [Status], [FulfillmentProviderKey], [IdempotencyKey], [CorrelationReference], [AttemptCount], [CreatedAt], [LastUpdateTime])
                VALUES
                    (70, 1, 2, {(int)OrderFulfillmentTaskType.ReserveInventory}, {(int)OrderFulfillmentStatus.Succeeded}, N'FlightFlow', N'reserve-hold:70', N'order:1:reservation:2', 1, {_clock.Now}, {_clock.Now});
                INSERT INTO [Order].[FulfillmentTaskTargets]
                    ([Id], [FulfillmentTaskId], [ReservationUnitId], [Action], [LastUpdateTime])
                VALUES
                    (71, 70, 3, {(int)OrderFulfillmentTargetAction.Reserve}, {_clock.Now}),
                    (72, 70, 4, {(int)OrderFulfillmentTargetAction.Reserve}, {_clock.Now});
                """);

            await migrator.MigrateAsync();
        }

        await using (var context = _database.NewContext())
        {
            var task = await new FulfillmentTaskRepository(context).FindLatestAsync(2, OrderFulfillmentTaskType.ReserveInventory);
            var legacyColumns = await context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Order' AND TABLE_NAME = 'FulfillmentTaskTargets' AND COLUMN_NAME = 'ReservationUnitId'")
                .SingleAsync();

            Assert.NotNull(task);
            Assert.Equal((70L, 2L), (task.Id, task.FulfillmentReservationId!.Value));
            Assert.Equal(
                [(71L, FulfillmentTargetKind.ReservationUnit, 3L, OrderFulfillmentTargetAction.Reserve), (72L, FulfillmentTargetKind.ReservationUnit, 4L, OrderFulfillmentTargetAction.Reserve)],
                task.Targets.OrderBy(target => target.Id).Select(target => (target.Id, target.TargetKind, target.TargetId, target.Action)));
            Assert.Equal(0, legacyColumns);
        }
    }
}
