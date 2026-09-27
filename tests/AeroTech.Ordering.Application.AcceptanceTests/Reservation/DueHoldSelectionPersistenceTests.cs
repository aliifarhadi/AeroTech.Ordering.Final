using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Persistence.FulfillmentReservationAggregate;
using AeroTech.Ordering.Persistence.FulfillmentTaskAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class DueHoldSelectionPersistenceTests : IAsyncLifetime
{
    private const string UnitKey = "unit";

    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public DueHoldSelectionPersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Only_holds_with_actionable_settlement_are_due()
    {
        var start = _clock.Now;
        var passedDeadline = start.AddMinutes(10);
        var openDeadline = start.AddHours(2);
        var lapsed = start.AddMinutes(20);
        var valid = start.AddHours(1);

        await using (var context = _database.NewContext())
        {
            var releasable = await SeedHeldAsync(context, passedDeadline, valid);
            var releaseRejected = await SeedHeldAsync(context, passedDeadline, valid);
            var holdLapsed = await SeedHeldAsync(context, openDeadline, lapsed);
            await SeedHeldAsync(context, openDeadline, valid);
            var confirmed = await SeedHeldAsync(context, passedDeadline, valid);

            confirmed.RecordConfirmation(new ConfirmationOutcome(ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Confirmed, null, null), start);
            await new FulfillmentTaskRepository(context).AddAsync(RejectedRelease(releaseRejected));
            await context.SaveChangesAsync();

            _clock.Now = start.AddMinutes(30);
            var due = await new FulfillmentReservationRepository(context).ListOrderIdsWithDueHoldsAsync(_clock.Now, 100);

            Assert.Equal([releasable.OrderId, holdLapsed.OrderId], due.Order());
        }
    }

    private async Task<FulfillmentReservation> SeedHeldAsync(OrderingDbContext context, DateTimeOffset lastTicketingDate, DateTimeOffset expiresAt)
    {
        var order = OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);
        var serviceIds = ReservationHarness.AirServices(order).Select(service => service.Id).ToList();

        var reservation = FulfillmentReservation.Create(
            _ids.NewId(),
            order.Id,
            FulfillmentProviderKeys.FlightFlow,
            ReservationMode.HoldThenConfirm,
            expiresAt,
            expiresAt,
            [new ReservationUnitIntent(UnitKey, serviceIds, new UnitDetails())],
            _ids,
            _clock.Now);

        reservation.RecordOutcome(
            new ReservationOutcome(
                ProviderOperationOutcome.Succeeded,
                $"HOLD-{reservation.Id}",
                reservation.IdempotencyKey,
                reservation.CorrelationReference,
                expiresAt,
                [new ReservationUnitOutcome(UnitKey, serviceIds, ReservationMemberStatus.Held, $"SHR-{reservation.Id}", null, null)],
                null,
                null),
            _clock.Now);

        await new OrderRepository(context).AddAsync(order);
        await new FulfillmentReservationRepository(context).AddAsync(reservation);

        return reservation;
    }

    private FulfillmentTask RejectedRelease(FulfillmentReservation reservation)
    {
        var intent = reservation.PrepareRelease();
        var task = FulfillmentTask.Create(
            _ids.NewId(),
            reservation.OrderId,
            reservation.Id,
            OrderFulfillmentTaskType.ReleaseReserved,
            reservation.FulfillmentProviderKey,
            intent.IdempotencyKey,
            reservation.CorrelationReference,
            reservation.Units.Select(unit => unit.Id).ToList(),
            OrderFulfillmentTargetAction.Release,
            _ids,
            _clock.Now);

        var failure = new ProviderFailure(FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Refused.", 400);

        task.StartAttempt(_ids, _clock.Now);
        task.RecordRequest(new ProviderRequest(ProviderInteractionType.ReleaseHold, null, null, intent.ProviderOperationRef), _ids, _clock.Now);
        task.RecordResponse(ProviderOperationOutcome.Rejected, intent.ProviderOperationRef, failure, new ProviderResponse(400, null), _clock.Now);
        task.CompleteAttempt(FulfillmentAttemptOutcome.Failed, failure, _clock.Now);

        return task;
    }

    private sealed record UnitDetails : ReservationUnitDetails;
}
