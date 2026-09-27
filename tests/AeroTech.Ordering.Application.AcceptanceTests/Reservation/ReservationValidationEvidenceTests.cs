using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ReservationValidationEvidenceTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public void Evidence_covers_a_subset_of_its_scope_at_its_version_before_it_expires()
    {
        var evidence = Evidence(2, _harness.Clock.Now.AddHours(1), 11, 12);

        Assert.True(evidence.Covers([11], 2, _harness.Clock.Now));
        Assert.True(evidence.Covers([11, 12], 2, _harness.Clock.Now.AddMinutes(59)));
    }

    [Fact]
    public void Evidence_of_another_commercial_version_is_stale_before_it_expires()
        => Assert.False(Evidence(1, _harness.Clock.Now.AddHours(1), 11).Covers([11], 2, _harness.Clock.Now));

    [Fact]
    public void Evidence_is_stale_from_its_validity_instant()
        => Assert.False(Evidence(1, _harness.Clock.Now, 11).Covers([11], 1, _harness.Clock.Now));

    [Fact]
    public void Evidence_does_not_cover_a_service_outside_its_validated_scope()
        => Assert.False(Evidence(1, _harness.Clock.Now.AddHours(1), 11).Covers([11, 12], 1, _harness.Clock.Now));

    [Theory]
    [InlineData(0, new long[] { 11 })]
    [InlineData(1, new long[0])]
    [InlineData(1, new long[] { 11, 11 })]
    public void Evidence_needs_a_positive_version_and_a_distinct_non_empty_scope(int commercialVersion, long[] serviceIds)
    {
        var exception = Assert.Throws<BusinessException>(() => new ReservationValidationEvidence(commercialVersion, _harness.Clock.Now, _harness.Clock.Now, serviceIds));

        Assert.Equal((2822, 500), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public async Task Recorded_evidence_is_projected_to_the_compatibility_timestamp()
    {
        var (order, reservation) = await HeldAsync();
        var evidence = Evidence(order.CommercialVersion, _harness.Clock.Now.AddHours(5), AirServiceIds(order));

        reservation.RecordValidation(evidence);

        Assert.Same(evidence, reservation.ValidationEvidence);
        Assert.Equal(evidence.ValidUntil, reservation.ReservationValidationTimeLimit);
    }

    [Fact]
    public async Task Legacy_timestamp_alone_is_not_current_validation()
    {
        var (order, reservation) = await HeldAsync();
        typeof(FulfillmentReservation).GetProperty(nameof(FulfillmentReservation.ValidationEvidence))!.SetValue(reservation, null);

        Assert.True(reservation.ReservationValidationTimeLimit > _harness.Clock.Now);
        Assert.False(reservation.HasCurrentValidationFor(AirServiceIds(order), order.CommercialVersion, _harness.Clock.Now));
    }

    [Fact]
    public async Task Reservation_without_live_capacity_cannot_record_validation()
    {
        var (order, reservation) = await HeldAsync();
        await _harness.ReleaseAsync(order, reservation.Id);

        var exception = Assert.Throws<BusinessException>(() => reservation.RecordValidation(Evidence(1, _harness.Clock.Now.AddHours(1), AirServiceIds(order))));

        Assert.Equal(2740, exception.Code);
    }

    [Fact]
    public async Task Held_confirmation_revalidates_evidence_of_an_older_commercial_version()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var reserved = await _harness.ReserveServicesAsync(order, outbound.Id);
        await _harness.ReserveServicesAsync(order, ReservationHarness.AirService(order, 1, 201).Id);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);
        var reservation = _harness.Reservation(reserved.Reservations.Single().ReservationId);
        var calls = _harness.AirFareValidator.Calls.Count;

        Assert.Equal((1, 2), (reservation.ValidationEvidence!.CommercialVersion, order.CommercialVersion));

        await _harness.ConfirmReservationsAsync(order, reservation.Id);

        Assert.Equal(calls + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal((2, FulfillmentReservationStatus.Confirmed), (reservation.ValidationEvidence!.CommercialVersion, reservation.Status));
    }

    private async Task<(Order Order, FulfillmentReservation Reservation)> HeldAsync()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var reserved = await _harness.ReserveOrderAsync(order);

        return (order, _harness.Reservation(reserved.Reservations.Single().ReservationId));
    }

    private ReservationValidationEvidence Evidence(int commercialVersion, DateTimeOffset validUntil, params long[] serviceIds)
        => new(commercialVersion, validUntil, _harness.Clock.Now, serviceIds);

    private static long[] AirServiceIds(Order order)
        => ReservationHarness.AirServices(order).Select(service => service.Id).ToArray();
}
