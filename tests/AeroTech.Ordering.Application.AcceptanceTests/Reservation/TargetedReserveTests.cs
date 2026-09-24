using System.Globalization;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class TargetedReserveTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Reserves_only_the_requested_uncovered_services()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);

        var result = await _harness.ReserveServicesAsync(order, outbound.Id);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        Assert.Equal([outbound.Id], reservation.CoveredOrderServiceIds);
        Assert.Equal(OrderFixture.CapacityIdOf(101).ToString(CultureInfo.InvariantCulture), _harness.FlightFlow.HoldRequests.Single().Flights.Single().FlightCapId);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Already_held_services_are_not_touched()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        await _harness.ReserveOrderAsync(order);

        var result = await _harness.ReserveServicesAsync(order, ReservationHarness.AirService(order, 1, 101).Id);

        Assert.Empty(result.Reservations);
        Assert.Single(_harness.FlightFlow.HoldRequests);
        Assert.Single(_harness.Reservations.Committed);
    }

    [Fact]
    public async Task Definitively_rejected_service_gets_a_new_operation_later()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var air = ReservationHarness.AirService(order, 1, 101);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "No seats available.", 422));

        var rejected = await _harness.ReserveOrderAsync(order);
        var retried = await _harness.ReserveServicesAsync(order, air.Id);

        var first = _harness.Reservation(rejected.Reservations.Single().ReservationId);
        var second = _harness.Reservation(retried.Reservations.Single().ReservationId);

        Assert.NotEqual(first.Id, second.Id);
        Assert.NotEqual(first.IdempotencyKey, second.IdempotencyKey);
        Assert.NotEqual(first.CorrelationReference, second.CorrelationReference);
        Assert.Equal(FulfillmentReservationStatus.Rejected, first.Status);
        Assert.Equal(FulfillmentReservationStatus.Held, second.Status);
        Assert.Equal(2, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Unknown_service_resumes_the_same_operation_without_revalidation()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var air = ReservationHarness.AirService(order, 1, 101);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The request timed out."));

        var unknown = await _harness.ReserveOrderAsync(order);
        _harness.Clock.Now = _harness.Clock.Now.AddMinutes(3);
        var resumed = await _harness.ReserveServicesAsync(order, air.Id);

        var reservation = _harness.Reservation(unknown.Reservations.Single().ReservationId);
        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory);
        var (first, second) = (_harness.FlightFlow.HoldRequests[0], _harness.FlightFlow.HoldRequests[1]);

        Assert.Equal(reservation.Id, resumed.Reservations.Single().ReservationId);
        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(first.IdempotencyKey, second.IdempotencyKey);
        Assert.Equal(first.Reference, second.Reference);
        Assert.Equal(first.ExpiresAt, second.ExpiresAt);
        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Equal(2, task.AttemptCount);
        Assert.Equal([FulfillmentAttemptOutcome.Unknown, FulfillmentAttemptOutcome.Succeeded], task.Attempts.OrderBy(attempt => attempt.AttemptNumber).Select(attempt => attempt.Outcome));
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Requested_service_must_belong_to_the_order_and_require_reservation()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveServicesAsync(order, 999));

        Assert.Equal(2732, exception.Code);
        Assert.Equal(404, exception.HttpStatus);
        Assert.Empty(_harness.FlightFlow.HoldRequests);
    }
}
