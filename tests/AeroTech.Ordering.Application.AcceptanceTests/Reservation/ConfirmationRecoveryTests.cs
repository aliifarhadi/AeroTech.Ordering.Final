using System.Net;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ConfirmationRecoveryTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Lost_confirmation_response_leaves_the_reservation_unknown()
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Unknown, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory).Status);
        Assert.Equal(FulfillmentFailureReason.UnknownOutcome, result.Reservations.Single().FailureReason);
        Assert.Single(_harness.FlightFlow.ConfirmRequests);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Repeated_confirmation_replays_the_exact_persisted_request_of_the_same_effect()
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);

        await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        var interactions = task.Interactions.ToList();
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal((OrderFulfillmentStatus.Succeeded, 2), (task.Status, task.AttemptCount));
        Assert.Single(interactions.Select(interaction => (interaction.RequestPayload, interaction.RequestHash)).Distinct());
        Assert.All(interactions, interaction => Assert.Null(interaction.IdempotencyKey));
        Assert.Single(_harness.FlightFlow.ConfirmRequests.Distinct());
        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Replay_after_the_last_ticketing_date_resolves_the_confirmation_without_reviving_the_order()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var (order, reservation) = await ReserveAsync(lastTicketingDate);
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);
        _harness.Clock.Now = lastTicketingDate;
        await _harness.EnforceDeadlinesAsync(order);

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
        Assert.Single(task.Interactions.Select(interaction => (interaction.RequestPayload, interaction.RequestHash)).Distinct());
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status);
        Assert.Null(result.Reservations.Single().FailureReason);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal((OrderStatus.Expired, OrderStatus.Expired), (result.Status, order.Status));
    }

    [Fact]
    public async Task Replay_after_the_hold_lapses_locally_resolves_the_confirmation()
    {
        var (order, reservation) = await ReserveAsync(holdExpiry: _harness.Clock.Now.AddMinutes(30));
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);

        _harness.Clock.Now = reservation.ExpiresAt!.Value;
        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory).Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Replay_that_proves_the_hold_expired_settles_the_reservation()
    {
        var (order, reservation) = await ReserveAsync(holdExpiry: _harness.Clock.Now.AddMinutes(30));
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => new ConfirmHoldResult(FulfillmentReservationStatus.Expired, "Cannot confirm an expired seat hold."));

        _harness.Clock.Now = reservation.ExpiresAt!.Value;
        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
        Assert.Equal(FulfillmentReservationStatus.Expired, reservation.Status);
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory).Status);
    }

    [Fact]
    public async Task Replay_with_stale_validation_calls_no_air_price()
    {
        var validUntil = _harness.Clock.Now.AddHours(1);
        var (order, reservation) = await ReserveAsync(validUntil: validUntil, holdExpiry: _harness.Clock.Now.AddHours(3));
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);

        _harness.Clock.Now = validUntil;
        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Equal(validUntil, reservation.ReservationValidationTimeLimit);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
    }

    [Fact]
    public async Task Unresolved_confirmation_without_a_persisted_request_is_not_replayed()
    {
        var (order, reservation) = await ReserveAsync();
        var task = FulfillmentTask.Create(
            _harness.Ids.NewId(),
            order.Id,
            reservation.Id,
            OrderFulfillmentTaskType.ConfirmInventory,
            reservation.FulfillmentProviderKey,
            "confirm-hold:unrecorded",
            reservation.CorrelationReference,
            FulfillmentTargetKind.ReservationUnit,
            reservation.Units.Select(unit => unit.Id).ToList(),
            OrderFulfillmentTargetAction.Confirm,
            _harness.Ids,
            _harness.Clock.Now);
        await _harness.Tasks.AddAsync(task);
        await _harness.UnitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal((2773, 500), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Same(task, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory));
        Assert.Equal((OrderFulfillmentStatus.Pending, 0, 0), (task.Status, task.AttemptCount, task.Interactions.Count));
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task Generic_rejection_of_a_replay_keeps_the_confirmation_unresolved(HttpStatusCode status)
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);
        _harness.FlightFlow.ConfirmWireResponses.Enqueue(FlightFlowWire.Response(status));

        await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.Equal((OrderFulfillmentStatus.Unknown, true), (task.Status, task.IsResumable));
        Assert.Equal<int?>((int)status, task.Interactions.Single(interaction => interaction.AttemptNumber == 2).ProviderStatusCode);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);

        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(3, _harness.FlightFlow.ConfirmRequests.Count);
        Assert.Equal((FulfillmentReservationStatus.Confirmed, OrderFulfillmentStatus.Succeeded), (reservation.Status, task.Status));
    }

    [Fact]
    public async Task Interrupted_confirmation_rejected_generically_on_replay_becomes_unknown()
    {
        var (order, reservation) = await ReserveAsync();
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new InvalidOperationException("The host stopped during the call."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _harness.ConfirmReservedCapacityAsync(order));
        _harness.FlightFlow.ConfirmWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.Forbidden));

        await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.Equal((OrderFulfillmentStatus.Unknown, true, 2), (task.Status, task.IsResumable, task.AttemptCount));
        Assert.Equal(2, _harness.FlightFlow.ConfirmRequests.Count);
    }

    [Fact]
    public async Task Plain_not_found_on_replay_keeps_the_confirmation_unresolved()
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);
        _harness.FlightFlow.ConfirmWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.NotFound));

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.Equal((OrderFulfillmentStatus.Unknown, true, 2), (task.Status, task.IsResumable, task.AttemptCount));
        Assert.Equal(FulfillmentFailureReason.UnknownOutcome, result.Reservations.Single().FailureReason);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Replay_that_stays_ambiguous_keeps_the_effect_unresolved()
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);
        LoseNextConfirmationResponse();

        await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.Equal((OrderFulfillmentStatus.Unknown, 2), (task.Status, task.AttemptCount));
    }

    [Fact]
    public async Task Provider_without_safe_replay_is_never_replayed_blindly()
    {
        var provider = new StubReservationProvider("ProviderB", ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held)
        {
            ValidationTimeLimit = DateTimeOffset.MaxValue
        };
        var harness = new ReservationHarness(provider);
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, provider.ProviderKey);
        var reservationId = (await harness.ReserveOrderAsync(order)).Reservations.Single().ReservationId;
        provider.ConfirmResponses.Enqueue(StubReservationProvider.UnobservedConfirmation);
        await harness.ConfirmReservedCapacityAsync(order);

        var repeated = await harness.ConfirmReservedCapacityAsync(order);

        Assert.Single(provider.ConfirmCalls);
        Assert.Equal(FulfillmentReservationStatus.Unknown, harness.Reservation(reservationId).Status);
        Assert.Equal(FulfillmentReservationStatus.Unknown, repeated.Reservations.Single().Status);
        Assert.Equal(OrderFulfillmentStatus.Unknown, harness.TaskOf(reservationId, OrderFulfillmentTaskType.ConfirmInventory).Status);
    }

    [Fact]
    public async Task Reserve_after_an_unknown_confirmation_never_replays_the_hold()
    {
        var (order, reservation) = await ReserveAsync();
        LoseNextConfirmationResponse();
        await _harness.ConfirmReservedCapacityAsync(order);

        var reserved = await _harness.ReserveOrderAsync(order);

        Assert.Empty(reserved.Reservations);
        Assert.Single(_harness.FlightFlow.HoldRequests);
        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory).Status);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
    }

    [Theory]
    [InlineData(false, FulfillmentReservationStatus.Held)]
    [InlineData(true, FulfillmentReservationStatus.Unknown)]
    public async Task One_provider_confirmation_survives_another_provider_not_confirming(
        bool otherOutcomeIsLost,
        FulfillmentReservationStatus otherStatus)
    {
        var other = new StubReservationProvider("ProviderB", ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held)
        {
            ValidationTimeLimit = DateTimeOffset.MaxValue
        };
        var harness = new ReservationHarness(other);
        var order = harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 2, other.ProviderKey);
        await harness.ReserveOrderAsync(order);
        other.ConfirmResponses.Enqueue(otherOutcomeIsLost ? StubReservationProvider.UnobservedConfirmation : StubReservationProvider.RejectedConfirmation);

        var result = await harness.ConfirmReservedCapacityAsync(order);

        var flightFlow = result.Reservations.Single(item => item.FulfillmentProviderKey == harness.FlightFlowReservation.ProviderKey);
        var provider = result.Reservations.Single(item => item.FulfillmentProviderKey == other.ProviderKey);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, harness.Reservation(flightFlow.ReservationId).Status);
        Assert.Equal(otherStatus, harness.Reservation(provider.ReservationId).Status);
        Assert.Empty(harness.FlightFlow.ReleaseRequests);
        Assert.Empty(other.ReleaseCalls);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    private async Task<(Order Order, FulfillmentReservation Reservation)> ReserveAsync(
        DateTimeOffset? lastTicketingDate = null,
        DateTimeOffset? validUntil = null,
        DateTimeOffset? holdExpiry = null)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);

        if (validUntil is { } timeLimit)
            _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        if (holdExpiry is { } expiresAt)
            _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { ExpiresAt = expiresAt });

        var result = await _harness.ReserveOrderAsync(order);

        return (order, _harness.Reservation(result.Reservations.Single().ReservationId));
    }

    private void LoseNextConfirmationResponse()
        => _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The seat-hold confirmation timed out."));
}
