using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ReleaseTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Held_operation_is_released_by_its_provider_operation_reference()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        var reservation = _harness.Reservation(held.Reservations.Single().ReservationId);

        var result = await _harness.ReleaseAsync(order, reservation.Id);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved);
        Assert.Equal(reservation.ProviderOperationRef, _harness.FlightFlow.ReleaseRequests.Single().HoldId);
        Assert.Equal(FulfillmentReservationStatus.Released, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Released, unit.Status));
        Assert.Equal(FulfillmentReservationStatus.Released, result.ReservationStatus);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status);
        Assert.Equal($"release-hold:{reservation.Id}", task.IdempotencyKey);
        Assert.Equal(ProviderInteractionType.ReleaseHold, task.Interactions.Single().InteractionType);
        Assert.All(task.Targets, target => Assert.Equal(OrderFulfillmentTargetAction.Release, target.Action));
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);
        Assert.Equal(OrderStatus.ReserveFailed, result.Status);
        Assert.Equal(OrderStatus.ReserveFailed, _harness.Synchronizer.ReservationProjections.Last().Status);
    }

    [Fact]
    public async Task Rejected_release_keeps_the_hold()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ReleaseResponses.Enqueue(_ => new ReleaseHeldSeatsResult(false, "Hold is already confirmed."));

        var result = await _harness.ReleaseAsync(order, held.Reservations.Single().ReservationId);

        Assert.Equal(FulfillmentReservationStatus.Held, result.ReservationStatus);
        Assert.Equal(FulfillmentFailureReason.ProviderRejected, result.FailureReason);
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(result.ReservationId, OrderFulfillmentTaskType.ReleaseReserved).Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Unknown_release_is_resumed_by_the_same_task()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        var reservationId = held.Reservations.Single().ReservationId;
        _harness.FlightFlow.ReleaseResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));

        var unknown = await _harness.ReleaseAsync(order, reservationId);
        var released = await _harness.ReleaseAsync(order, reservationId);

        var task = _harness.TaskOf(reservationId, OrderFulfillmentTaskType.ReleaseReserved);
        Assert.Equal(FulfillmentReservationStatus.Held, unknown.ReservationStatus);
        Assert.Equal(FulfillmentReservationStatus.Released, released.ReservationStatus);
        Assert.Equal(2, task.AttemptCount);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status);
    }

    [Fact]
    public async Task Only_a_held_reservation_can_be_released()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Rejected.", 422));
        var rejected = await _harness.ReserveOrderAsync(order);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _harness.ReleaseAsync(order, rejected.Reservations.Single().ReservationId));

        Assert.Equal(2739, exception.Code);
        Assert.Equal(409, exception.HttpStatus);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
    }

    [Fact]
    public async Task Reservation_of_another_order_is_not_found()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var other = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 102)]);
        var held = await _harness.ReserveOrderAsync(other);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => _harness.ReleaseAsync(order, held.Reservations.Single().ReservationId));

        Assert.Equal(2738, exception.Code);
        Assert.Equal(404, exception.HttpStatus);
    }
}
