using System.Globalization;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class BasicAirReservationTests
{
    public static TheoryData<string, TravellerSpec[], BoundSpec[]> Itineraries => new()
    {
        { "1 ADT / 1 flight", [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)] },
        { "multiple passengers", [TravellerSpec.Adult(1), TravellerSpec.Adult(2), TravellerSpec.Child(3)], [new BoundSpec("OUT", 101)] },
        { "round trip", [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)] },
        { "connection", [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)] },
        { "multiple bounds", [TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("B1", 101), new BoundSpec("B2", 201, 202), new BoundSpec("B3", 301)] }
    };

    [Theory]
    [MemberData(nameof(Itineraries))]
    public async Task Every_passenger_flight_unit_is_held_in_one_operation(string itinerary, TravellerSpec[] travellers, BoundSpec[] bounds)
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder(travellers, bounds);
        var flightIds = bounds.SelectMany(bound => bound.FlightIds).ToList();

        var result = await harness.ReserveOrderAsync(order);

        var reservation = harness.Reservation(result.Reservations.Single().ReservationId);
        var request = harness.FlightFlow.HoldRequests.Single();

        Assert.True(reservation.Status == FulfillmentReservationStatus.Held, itinerary);
        Assert.Equal(travellers.Length * flightIds.Count, reservation.Units.Count);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Held, unit.Status));
        Assert.Equal(travellers.Length, request.Passengers.Count);
        Assert.Equal(
            flightIds.Select(flightId => OrderFixture.CapacityIdOf(flightId).ToString(CultureInfo.InvariantCulture)).Order(),
            request.Flights.Select(flight => flight.FlightCapId).Order());
        Assert.All(request.Flights, flight => Assert.Equal(travellers.Length, flight.Seats.Count));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.RecordLocator);
    }

    [Fact]
    public async Task Passenger_reference_is_the_order_traveller_identity()
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        await harness.ReserveOrderAsync(order);

        var passenger = harness.FlightFlow.HoldRequests.Single().Passengers.Single();
        Assert.Equal(order.Travellers.Single().Id.ToString(CultureInfo.InvariantCulture), passenger.PaxReference);
        Assert.Equal(AeroTech.Messages.AirPrice.Enums.PassengerTypeCode.ADT, passenger.Type);
        Assert.Equal(0m, harness.FlightFlow.HoldRequests.Single().Flights.Single().Seats.Single().Revenue);
    }
}
