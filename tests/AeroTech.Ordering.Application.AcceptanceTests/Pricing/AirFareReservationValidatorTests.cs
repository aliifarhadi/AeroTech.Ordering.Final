using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Pricing;
using AeroTech.Ordering.Providers.Pricing.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Pricing;

public sealed class AirFareReservationValidatorTests
{
    private readonly ReservationHarness _harness = new();
    private readonly RecordingPricingProvider _pricing = new();

    [Fact]
    public async Task One_way_fare_per_bound_is_validated_as_one_one_way_pricing_unit_per_bound()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);

        var timeLimit = await ValidateAsync(order, ReservationHarness.AirServices(order));

        var request = _pricing.Requests.Single();
        Assert.Equal(_pricing.TimeLimit, timeLimit);
        Assert.Equal(new AirFareBoundReservationSalesContext(7, null, SalesChannel.BackOffice, 42, _harness.Clock.Now, 1), request.SalesContext);
        Assert.Equal([PassengerTypeCode.ADT, PassengerTypeCode.ADT], request.Passengers.Select(passenger => passenger.PassengerTypeCode));
        Assert.Equal(
            [
                (JourneyType.OneWay, OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 1), "OUT", "7001", "101"),
                (JourneyType.OneWay, OrderFixture.AirportOf(2, 0), OrderFixture.AirportOf(2, 1), "IN", "7002", "201")
            ],
            request.PricingUnits.Select(unit => (
                unit.JourneyType,
                unit.PricingOriginAirportId,
                unit.PricingDestinationAirportId,
                unit.BoundIds.Single(),
                unit.AirFares.Single().AirFareId,
                unit.AirFares.Single().Flights.Single().FlightId)));

        var segment = order.Segments.Single(item => item.FlightId == 101);
        Assert.Equal(
            new AirFareBoundReservationFlight("101", "DA101", segment.OriginAirportId, segment.DestinationAirportId, OrderFixture.AircraftId, segment.SoldDeparture, FlightStatus.Open, null, OrderFixture.RbdId, 2),
            request.PricingUnits[0].AirFares.Single().Flights.Single());
    }

    [Fact]
    public async Task Fare_shared_by_outbound_and_inbound_is_a_round_trip_pricing_unit_on_the_outbound()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            fares: [new FareSpec(9001, 101, 201)]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        var unit = _pricing.Requests.Single().PricingUnits.Single();
        Assert.Equal(JourneyType.RoundTrip, unit.JourneyType);
        Assert.Equal((OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 1)), (unit.PricingOriginAirportId, unit.PricingDestinationAirportId));
        Assert.Equal(["OUT", "IN"], unit.BoundIds);
        Assert.Equal(["101", "201"], unit.AirFares.Single().Flights.Select(flight => flight.FlightId));
    }

    [Fact]
    public async Task Round_trip_fare_keeps_its_journey_type_when_only_one_direction_is_validated()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            fares: [new FareSpec(9001, 101, 201)]);

        await ValidateAsync(order, [ReservationHarness.AirService(order, 1, 101)]);

        var unit = _pricing.Requests.Single().PricingUnits.Single();
        Assert.Equal(JourneyType.RoundTrip, unit.JourneyType);
        Assert.Equal(["101"], unit.AirFares.Single().Flights.Select(flight => flight.FlightId));
    }

    [Fact]
    public async Task Sector_fares_on_one_bound_are_one_way_units_over_their_own_flights()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            fares: [new FareSpec(8001, 101), new FareSpec(8002, 102)]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        Assert.Equal([8001L, 8002L], ReservationHarness.AirServices(order).Select(service => service.AirFareId!.Value));
        Assert.Equal(
            [
                (JourneyType.OneWay, OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 1), "8001", "101"),
                (JourneyType.OneWay, OrderFixture.AirportOf(1, 1), OrderFixture.AirportOf(1, 2), "8002", "102")
            ],
            _pricing.Requests.Single().PricingUnits.Select(unit => (
                unit.JourneyType,
                unit.PricingOriginAirportId,
                unit.PricingDestinationAirportId,
                unit.AirFares.Single().AirFareId,
                unit.AirFares.Single().Flights.Single().FlightId)));
    }

    [Fact]
    public async Task Lap_infant_is_a_passenger_but_does_not_consume_a_seat()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101)]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        var request = _pricing.Requests.Single();
        Assert.Equal([PassengerTypeCode.ADT, PassengerTypeCode.INF], request.Passengers.Select(passenger => passenger.PassengerTypeCode));
        Assert.Equal(1, request.PricingUnits.Single().AirFares.Single().Flights.Single().RequiredSeats);
    }

    [Fact]
    public async Task Air_service_created_without_an_rbd_cannot_be_validated()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var air = ReservationHarness.AirService(order, 1, 101);
        typeof(OrderAirTransportService).GetProperty(nameof(OrderAirTransportService.RbdId))!.SetValue(air, null);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(order, [air]));

        Assert.Equal(2737, exception.Code);
        Assert.Equal(422, exception.HttpStatus);
        Assert.Empty(_pricing.Requests);
    }

    private Task<DateTimeOffset> ValidateAsync(Order order, IReadOnlyList<OrderAirTransportService> airServices)
        => new AirFareReservationValidator(_pricing, _harness.Clock).ValidateAsync(order, airServices.Select(service => service.Id).ToList());
}
