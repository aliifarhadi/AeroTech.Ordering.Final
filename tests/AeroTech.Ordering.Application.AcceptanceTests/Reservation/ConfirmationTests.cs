using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Domain._Shared.Resources;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ConfirmationTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Fresh_held_reservation_is_confirmed_once_by_its_hold_reference()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        var evidence = (reservation.ProviderOperationRef, reservation.RequestedExpiresAt, reservation.ExpiresAt, reservation.ReservationValidationTimeLimit);
        var unitRefs = reservation.Units.Select(unit => unit.ProviderUnitRef).ToList();

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory);
        var interaction = task.Interactions.Single();
        Assert.Equal(reservation.ProviderOperationRef, _harness.FlightFlow.ConfirmRequests.Single().HoldId);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Confirmed, unit.Status));
        Assert.Equal(evidence, (reservation.ProviderOperationRef, reservation.RequestedExpiresAt, reservation.ExpiresAt, reservation.ReservationValidationTimeLimit));
        Assert.Equal(unitRefs, reservation.Units.Select(unit => unit.ProviderUnitRef).ToList());
        Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status);
        Assert.Equal($"confirm-hold:{task.Id}", task.IdempotencyKey);
        Assert.All(task.Targets, target => Assert.Equal(OrderFulfillmentTargetAction.Confirm, target.Action));
        Assert.Equal(ProviderInteractionType.ConfirmHold, interaction.InteractionType);
        Assert.Null(interaction.IdempotencyKey);
        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal((OrderStatus.Confirmed, FulfillmentReservationStatus.Confirmed), (result.Status, result.Reservations.Single().Status));
        Assert.Equal(OrderStatus.Confirmed, _harness.Synchronizer.ReservationProjections.Last().Status);
    }

    [Fact]
    public async Task Stale_validation_is_renewed_once_before_the_confirmation()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: _harness.Clock.Now.AddHours(3));
        var renewedTimeLimit = timeLimit.AddHours(1);

        _harness.Clock.Now = timeLimit;
        _harness.AirFareValidator.Behavior = (_, _) => renewedTimeLimit;
        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(2, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(renewedTimeLimit, reservation.ReservationValidationTimeLimit);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Single(_harness.FlightFlow.ConfirmRequests);
    }

    [Fact]
    public async Task Reservation_without_validation_evidence_is_revalidated_before_the_confirmation()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        typeof(FulfillmentReservation).GetProperty(nameof(FulfillmentReservation.ReservationValidationTimeLimit))!.SetValue(reservation, null);

        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(2, _harness.AirFareValidator.Calls.Count);
        Assert.NotNull(reservation.ReservationValidationTimeLimit);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public async Task Rejected_revalidation_leaves_the_hold_unconfirmed()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: _harness.Clock.Now.AddHours(3));

        _harness.Clock.Now = timeLimit;
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FareReservationIsNotPermitted("The fare is no longer valid.");
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal(2608, exception.Code);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
        Assert.Single(_harness.Reservations.Committed);
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory);
    }

    [Fact]
    public async Task Unsupported_fare_semantic_fails_closed_before_the_provider()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: _harness.Clock.Now.AddHours(3));

        _harness.Clock.Now = timeLimit;
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FarePricingUnitValidationIsUnsupported(1, "SectorSum");
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal(2768, exception.Code);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Lapsed_hold_is_not_confirmed()
    {
        var order = SeedOrder();
        var holdExpiry = _harness.Clock.Now.AddMinutes(30);
        var reservation = await ReserveAsync(order, _harness.Clock.Now.AddHours(1), holdExpiry);

        _harness.Clock.Now = holdExpiry;
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal((2771, 409), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Renewed_validation_does_not_revive_a_lapsed_hold()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddMinutes(30);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: timeLimit);

        _harness.Clock.Now = timeLimit;
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit.AddHours(2);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal(2771, exception.Code);
        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Passed_last_ticketing_date_rejects_the_confirmation_before_any_provider()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = SeedOrder(lastTicketingDate);
        var reservation = await ReserveAsync(order, _harness.Clock.Now.AddHours(1), holdExpiry: _harness.Clock.Now.AddHours(2));

        _harness.Clock.Now = lastTicketingDate;
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservedCapacityAsync(order));

        Assert.Equal((2770, 422), (exception.Code, exception.HttpStatus));
        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Confirmed_reservation_is_not_confirmed_again()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        var repeated = await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Single(_harness.FlightFlow.ConfirmRequests);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, repeated.Reservations.Single().Status);
        Assert.Equal(reservation.Id, repeated.Reservations.Single().ReservationId);
    }

    [Fact]
    public async Task Immediately_confirmed_reservation_needs_no_confirmation()
    {
        var immediate = new StubReservationProvider("ProviderB", ReservationMode.ImmediateConfirm, ReservationMemberStatus.Confirmed);
        var harness = new ReservationHarness(immediate);
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, immediate.ProviderKey);
        await harness.ReserveOrderAsync(order);

        var result = await harness.ConfirmReservedCapacityAsync(order);

        Assert.Empty(immediate.ConfirmCalls);
        Assert.DoesNotContain(harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, result.Reservations.Single().Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Definitive_rejection_keeps_the_hold_without_releasing_it()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.BusinessRejected, "Cannot confirm a seat hold whose seats are in inconsistent statuses.", 400));

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Held, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory).Status);
        Assert.Equal(FulfillmentFailureReason.BusinessRejected, result.Reservations.Single().FailureReason);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Theory]
    [InlineData(FlightSeatHoldStatus.Expired, FulfillmentReservationStatus.Expired, ReservationMemberStatus.Expired, FulfillmentFailureReason.HoldExpired)]
    [InlineData(FlightSeatHoldStatus.Released, FulfillmentReservationStatus.Released, ReservationMemberStatus.Released, FulfillmentFailureReason.ProviderRejected)]
    [InlineData(FlightSeatHoldStatus.Cancelled, FulfillmentReservationStatus.Cancelled, ReservationMemberStatus.Cancelled, FulfillmentFailureReason.ProviderRejected)]
    public async Task Provider_reported_hold_state_is_applied_when_the_confirmation_is_refused(
        FlightSeatHoldStatus reported,
        FulfillmentReservationStatus reservationStatus,
        ReservationMemberStatus unitStatus,
        FulfillmentFailureReason failureReason)
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => new ConfirmHoldResult(reported, "Cannot confirm the seat hold."));

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(reservationStatus, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(unitStatus, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory).Status);
        Assert.Equal(failureReason, result.Reservations.Single().FailureReason);
    }

    [Fact]
    public async Task Explicit_retry_after_a_definitive_rejection_is_a_new_confirmation_effect()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.BusinessRejected, "Refused.", 400));
        await _harness.ConfirmReservedCapacityAsync(order);

        await _harness.ConfirmReservedCapacityAsync(order);

        var tasks = _harness.Tasks.Committed.Where(task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory).ToList();
        Assert.Equal(2, tasks.Count);
        Assert.Equal(2, tasks.Select(task => task.IdempotencyKey).Distinct().Count());
        Assert.Equal([OrderFulfillmentStatus.Failed, OrderFulfillmentStatus.Succeeded], tasks.Select(task => task.Status));
        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public async Task Provider_success_is_recorded_even_when_the_deadline_passes_during_the_call()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = SeedOrder(lastTicketingDate);
        var reservation = await ReserveAsync(order, _harness.Clock.Now.AddHours(1));
        _harness.FlightFlow.ConfirmResponses.Enqueue(request =>
        {
            _harness.Clock.Now = lastTicketingDate.AddMinutes(1);
            return ScriptedFlightFlowProvider.Confirmed(request);
        });

        await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal(lastTicketingDate, order.LastTicketingDate);
    }

    [Fact]
    public async Task Order_without_reservations_awaiting_confirmation_is_a_no_op()
    {
        var order = SeedOrder();

        var result = await _harness.ConfirmReservedCapacityAsync(order);

        Assert.Empty(result.Reservations);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.Empty(_harness.AirFareValidator.Calls);
    }

    [Fact]
    public async Task Targeted_confirmation_leaves_other_reservations_untouched()
    {
        var other = new StubReservationProvider("ProviderB", ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held)
        {
            ValidationTimeLimit = DateTimeOffset.MaxValue
        };
        var harness = new ReservationHarness(other);
        var order = harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 2, other.ProviderKey);
        var reserved = await harness.ReserveOrderAsync(order);
        var flightFlowId = reserved.Reservations.Single(item => item.FulfillmentProviderKey == harness.FlightFlowReservation.ProviderKey).ReservationId;
        var otherId = reserved.Reservations.Single(item => item.FulfillmentProviderKey == other.ProviderKey).ReservationId;

        var result = await harness.ConfirmReservationsAsync(order, flightFlowId);

        Assert.Equal(flightFlowId, result.Reservations.Single().ReservationId);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, harness.Reservation(flightFlowId).Status);
        Assert.Equal(FulfillmentReservationStatus.Held, harness.Reservation(otherId).Status);
        Assert.Empty(other.ConfirmCalls);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Targeted_reservation_of_another_order_is_not_found()
    {
        var order = SeedOrder();
        var other = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 102)]);
        var reservation = await ReserveAsync(other);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservationsAsync(order, reservation.Id));

        Assert.Equal((2738, 404), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public async Task Targeted_terminal_reservation_is_not_confirmable()
    {
        var order = SeedOrder();
        var reservation = await ReserveAsync(order);
        await _harness.ReleaseAsync(order, reservation.Id);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservationsAsync(order, reservation.Id));

        Assert.Equal((2769, 409), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
    }

    private Order SeedOrder(DateTimeOffset? lastTicketingDate = null)
        => _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);

    private Task<FulfillmentReservation> ReserveAsync(Order order)
        => ReserveAsync(order, _harness.Clock.Now.AddHours(1));

    private async Task<FulfillmentReservation> ReserveAsync(Order order, DateTimeOffset timeLimit, DateTimeOffset? holdExpiry = null)
    {
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        if (holdExpiry is { } expiresAt)
            _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { ExpiresAt = expiresAt });

        var result = await _harness.ReserveOrderAsync(order);

        return _harness.Reservation(result.Reservations.Single().ReservationId);
    }
}
