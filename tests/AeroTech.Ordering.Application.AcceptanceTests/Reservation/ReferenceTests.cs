using System.Globalization;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ReferenceTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Hold_id_is_stored_once_as_the_provider_operation_reference()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request =>
        {
            var held = _harness.FlightFlow.Held(request);
            return held with { HoldId = "HOLD-FIRST", Seats = [] };
        });
        _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { HoldId = "HOLD-SECOND" });

        var unknown = await _harness.ReserveOrderAsync(order);
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal(2742, exception.Code);
        Assert.Equal("HOLD-FIRST", _harness.Reservation(unknown.Reservations.Single().ReservationId).ProviderOperationRef);
    }

    [Fact]
    public async Task Seat_hold_reference_is_stored_per_FlightFlow_unit()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101, 102)]);

        var result = await _harness.ReserveOrderAsync(order);

        var units = _harness.Reservation(result.Reservations.Single().ReservationId).Units;
        Assert.All(units, unit => Assert.Equal($"SHR-{unit.UnitCorrelationKey.Replace(':', '-')}", unit.ProviderUnitRef));
        Assert.Equal(units.Count, units.Select(unit => unit.ProviderUnitRef).Distinct().Count());
    }

    [Fact]
    public async Task Order_reference_record_locator_operation_and_unit_references_stay_distinct()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        var references = new[]
        {
            order.OrderReference.ToString(),
            order.RecordLocator,
            reservation.ProviderOperationRef,
            reservation.Units.Single().ProviderUnitRef
        };

        Assert.All(references, reference => Assert.NotNull(reference));
        Assert.Equal(references.Length, references.Distinct().Count());
        Assert.NotEqual(reservation.IdempotencyKey, reservation.CorrelationReference);
        Assert.Equal($"reserve-hold:{reservation.Id}", reservation.IdempotencyKey);
        Assert.Equal(
            $"order:{order.Id.ToString(CultureInfo.InvariantCulture)}:reservation:{reservation.Id.ToString(CultureInfo.InvariantCulture)}",
            reservation.CorrelationReference);
    }
}
