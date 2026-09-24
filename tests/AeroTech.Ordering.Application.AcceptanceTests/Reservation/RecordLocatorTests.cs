using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class RecordLocatorTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task First_positive_air_hold_generates_one_record_locator()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var result = await _harness.ReserveOrderAsync(order);

        Assert.Equal("PNR001", order.RecordLocator);
        Assert.Equal("PNR001", result.RecordLocator);
        Assert.Equal("PNR001", _harness.Synchronizer.ReservationProjections.Last().RecordLocator);
        Assert.Single(_harness.RecordLocators.Generated);
    }

    [Fact]
    public async Task Later_reservation_success_keeps_the_same_record_locator()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);

        await _harness.ReserveServicesAsync(order, ReservationHarness.AirService(order, 1, 101).Id);
        await _harness.ReserveServicesAsync(order, ReservationHarness.AirService(order, 1, 201).Id);

        Assert.Equal("PNR001", order.RecordLocator);
        Assert.Single(_harness.RecordLocators.Generated);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Unknown_outcome_and_its_retry_never_create_a_second_record_locator()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));

        await _harness.ReserveOrderAsync(order);
        var afterUnknown = order.RecordLocator;
        await _harness.ReserveOrderAsync(order);
        await _harness.ReserveOrderAsync(order);

        Assert.Null(afterUnknown);
        Assert.Equal("PNR001", order.RecordLocator);
        Assert.Single(_harness.RecordLocators.Generated);
    }

    [Fact]
    public async Task Release_and_expiry_do_not_clear_the_record_locator()
    {
        var released = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var expired = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 102)]);

        var releasedHold = await _harness.ReserveOrderAsync(released);
        var expiredHold = await _harness.ReserveOrderAsync(expired);
        await _harness.ReleaseAsync(released, releasedHold.Reservations.Single().ReservationId);
        _harness.Reservation(expiredHold.Reservations.Single().ReservationId).RecordExpired(_harness.Clock.Now);

        Assert.Equal("PNR001", released.RecordLocator);
        Assert.Equal("PNR002", expired.RecordLocator);
    }

    [Fact]
    public async Task Rejected_reservation_generates_no_record_locator()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Rejected.", 422));

        await _harness.ReserveOrderAsync(order);

        Assert.Null(order.RecordLocator);
        Assert.Empty(_harness.RecordLocators.Generated);
    }

    [Fact]
    public async Task Record_locator_already_in_use_is_never_reused()
    {
        var existing = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        existing.AssignRecordLocator("PNR001");
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 102)]);

        await _harness.ReserveOrderAsync(order);

        Assert.Equal("PNR002", order.RecordLocator);
    }
}
