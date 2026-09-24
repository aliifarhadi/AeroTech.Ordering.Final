using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class IdempotencyTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Duplicate_reserve_does_not_duplicate_the_positive_booking()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        await _harness.ReserveOrderAsync(order);
        var duplicate = await _harness.ReserveOrderAsync(order);

        Assert.Empty(duplicate.Reservations);
        Assert.Single(_harness.FlightFlow.HoldRequests);
        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(OrderStatus.Confirmed, duplicate.Status);
    }

    [Fact]
    public async Task Automatic_reserve_resumes_an_unknown_operation_with_the_same_idempotency()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Retriable, FulfillmentFailureReason.TechnicalFailed, "Service unavailable.", 503));

        await _harness.ReserveOrderAsync(order);
        await _harness.ReserveOrderAsync(order);

        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(_harness.FlightFlow.HoldRequests[0].IdempotencyKey, _harness.FlightFlow.HoldRequests[1].IdempotencyKey);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Released_reservation_is_retried_with_a_new_idempotency_key()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        await _harness.ReleaseAsync(order, held.Reservations.Single().ReservationId);

        var retried = await _harness.ReserveOrderAsync(order);

        AssertNewBusinessAttempt(order, held.Reservations.Single().ReservationId, retried.Reservations.Single().ReservationId);
    }

    [Fact]
    public async Task Expired_reservation_is_retried_with_a_new_idempotency_key()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var held = await _harness.ReserveOrderAsync(order);
        _harness.Reservation(held.Reservations.Single().ReservationId).RecordExpired(_harness.Clock.Now.AddMinutes(20));

        var retried = await _harness.ReserveOrderAsync(order);

        AssertNewBusinessAttempt(order, held.Reservations.Single().ReservationId, retried.Reservations.Single().ReservationId);
    }

    [Fact]
    public async Task Rejected_reservation_is_retried_with_a_new_idempotency_key()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Rejected.", 422));
        var rejected = await _harness.ReserveOrderAsync(order);

        var retried = await _harness.ReserveOrderAsync(order);

        AssertNewBusinessAttempt(order, rejected.Reservations.Single().ReservationId, retried.Reservations.Single().ReservationId);
    }

    [Fact]
    public async Task Intent_is_persisted_before_the_provider_is_called()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request =>
        {
            var reservation = Assert.Single(_harness.Reservations.Committed);
            var task = Assert.Single(_harness.Tasks.Committed);

            Assert.Equal(FulfillmentReservationStatus.Pending, reservation.Status);
            Assert.Equal(OrderFulfillmentStatus.InProgress, task.Status);
            Assert.Equal(reservation.IdempotencyKey, request.IdempotencyKey);

            return _harness.FlightFlow.Held(request);
        });

        await _harness.ReserveOrderAsync(order);

        Assert.Equal(1, _harness.FlightFlow.SaveCountAtHold.Single());
    }

    [Fact]
    public async Task Concurrent_operation_on_the_same_order_is_refused()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.Lock.Hold($"reservation:{order.Id}");

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal(2512, exception.Code);
        Assert.Equal(409, exception.HttpStatus);
        Assert.Empty(_harness.FlightFlow.HoldRequests);
    }

    private void AssertNewBusinessAttempt(Order order, long previousReservationId, long retriedReservationId)
    {
        var previous = _harness.Reservation(previousReservationId);
        var retried = _harness.Reservation(retriedReservationId);

        Assert.NotEqual(previous.Id, retried.Id);
        Assert.NotEqual(previous.IdempotencyKey, retried.IdempotencyKey);
        Assert.NotEqual(_harness.FlightFlow.HoldRequests[0].IdempotencyKey, _harness.FlightFlow.HoldRequests[^1].IdempotencyKey);
        Assert.Equal(FulfillmentReservationStatus.Held, retried.Status);
        Assert.Equal(2, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }
}
