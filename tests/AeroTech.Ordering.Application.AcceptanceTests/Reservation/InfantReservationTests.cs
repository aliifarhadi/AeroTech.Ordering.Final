using System.Globalization;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class InfantReservationTests
{
    [Fact]
    public async Task Lap_infant_is_left_out_of_the_hold_and_covered_by_the_parent_unit()
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101)]);
        var parent = order.Travellers.Single(traveller => traveller.Index == 1);

        var result = await harness.ReserveOrderAsync(order);

        var request = harness.FlightFlow.HoldRequests.Single();
        var unit = harness.Reservation(result.Reservations.Single().ReservationId).Units.Single();

        Assert.Equal([parent.Id.ToString(CultureInfo.InvariantCulture)], request.Passengers.Select(passenger => passenger.PaxReference));
        Assert.Equal([parent.Id.ToString(CultureInfo.InvariantCulture)], request.Flights.Single().Seats.Select(seat => seat.PaxReference));
        Assert.Equal(
            ReservationHarness.AirServices(order).Select(service => service.Id).Order(),
            unit.OrderServiceIds.Order());
        Assert.Equal(ReservationMemberStatus.Held, unit.Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }
}
