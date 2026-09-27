using System.Net;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
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
    public async Task Generic_rejection_of_a_release_keeps_the_hold()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ReleaseWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.Forbidden));

        var result = await _harness.ReleaseAsync(order, held.Reservations.Single().ReservationId);

        Assert.Equal(FulfillmentReservationStatus.Held, result.ReservationStatus);
        Assert.Equal(FulfillmentFailureReason.ProviderRejected, result.FailureReason);
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(result.ReservationId, OrderFulfillmentTaskType.ReleaseReserved).Status);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Theory]
    [InlineData(1181, FulfillmentReservationStatus.Unknown, ReservationMemberStatus.Unknown)]
    [InlineData(1182, FulfillmentReservationStatus.Confirmed, ReservationMemberStatus.Confirmed)]
    [InlineData(1183, FulfillmentReservationStatus.Expired, ReservationMemberStatus.Expired)]
    [InlineData(1184, FulfillmentReservationStatus.Cancelled, ReservationMemberStatus.Cancelled)]
    [InlineData(1185, FulfillmentReservationStatus.Mixed, ReservationMemberStatus.Unknown)]
    public async Task Release_refused_with_provider_state_records_that_state_and_never_released(
        int errorCode,
        FulfillmentReservationStatus reservationStatus,
        ReservationMemberStatus unitStatus)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        var reservation = _harness.Reservation(held.Reservations.Single().ReservationId);
        _harness.FlightFlow.ReleaseWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(errorCode)));

        var result = await _harness.ReleaseAsync(order, reservation.Id);

        Assert.Equal((reservationStatus, reservationStatus), (reservation.Status, result.ReservationStatus));
        Assert.All(reservation.Units, unit => Assert.Equal(unitStatus, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Failed, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved).Status);
        Assert.NotNull(result.FailureReason);
        Assert.Equal(order.Status, _harness.Synchronizer.ReservationProjections.Last().Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    public async Task Undocumented_success_of_a_release_is_not_recorded_as_released(HttpStatusCode status)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ReleaseWireResponses.Enqueue(FlightFlowWire.Response(status));

        var result = await _harness.ReleaseAsync(order, held.Reservations.Single().ReservationId);

        var task = _harness.TaskOf(result.ReservationId, OrderFulfillmentTaskType.ReleaseReserved);
        Assert.Equal(FulfillmentReservationStatus.Held, result.ReservationStatus);
        Assert.Equal(FulfillmentFailureReason.UnknownOutcome, result.FailureReason);
        Assert.Equal((OrderFulfillmentStatus.Unknown, true), (task.Status, task.IsResumable));
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Replayed_release_after_a_lost_response_records_one_release()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        var reservationId = held.Reservations.Single().ReservationId;
        _harness.FlightFlow.ReleaseWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.Accepted));
        await _harness.ReleaseAsync(order, reservationId);

        var released = await _harness.ReleaseAsync(order, reservationId);

        var task = _harness.TaskOf(reservationId, OrderFulfillmentTaskType.ReleaseReserved);
        Assert.Equal(FulfillmentReservationStatus.Released, released.ReservationStatus);
        Assert.Equal((OrderFulfillmentStatus.Succeeded, 2), (task.Status, task.AttemptCount));
        Assert.Single(task.Interactions.Select(interaction => (interaction.RequestPayload, interaction.RequestHash)).Distinct());
        Assert.Equal(2, _harness.FlightFlow.ReleaseRequests.Count);
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);
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
