using System.Reflection;
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
using FarePricingUnitType = AeroTech.Messages.Ordering.Enums.FarePricingUnitType;

namespace AeroTech.Ordering.Application.AcceptanceTests.Pricing;

public sealed class AirFareReservationValidatorTests
{
    private readonly ReservationHarness _harness = new();
    private readonly RecordingPricingProvider _pricing = new();

    [Fact]
    public async Task Round_trip_pricing_unit_is_validated_as_one_pricing_unit_scope()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            pricingUnits: [PricingUnitSpec.RoundTripFare(["OUT", "IN"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        var unit = Assert.Single(_pricing.Requests.Single().PricingUnits);
        Assert.Equal(order.FarePricingUnits.Single().Id.ToString(), unit.PricingUnitId);
        Assert.Equal(JourneyType.RoundTrip, unit.JourneyType);
        Assert.Equal(["OUT", "IN"], unit.BoundIds);
        Assert.Equal((OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 1)), (unit.PricingOriginAirportId, unit.PricingDestinationAirportId));
        Assert.Equal([("9001", "101"), ("9002", "201")], unit.AirFares.Select(fare => (fare.AirFareId, fare.Flights.Single().FlightId)));
    }

    [Fact]
    public async Task Two_one_way_pricing_units_are_validated_as_two_pricing_unit_scopes()
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
    public async Task Connecting_through_fare_is_one_air_fare_over_both_segments()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        var unit = Assert.Single(_pricing.Requests.Single().PricingUnits);
        Assert.Equal(JourneyType.OneWay, unit.JourneyType);
        Assert.Equal((OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 2)), (unit.PricingOriginAirportId, unit.PricingDestinationAirportId));
        Assert.Equal(["101", "102"], Assert.Single(unit.AirFares).Flights.Select(flight => flight.FlightId));
    }

    [Fact]
    public async Task Sector_fares_of_a_one_way_pricing_unit_are_validated_as_one_air_price_pricing_unit_each()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            pricingUnits: [PricingUnitSpec.ThroughOneWay("OUT", new FareSpec(8001, 101), new FareSpec(8002, 102))]);

        await ValidateAsync(order, [ReservationHarness.AirService(order, 1, 101)]);

        Assert.Equal(
            [
                (JourneyType.OneWay, OrderFixture.AirportOf(1, 0), OrderFixture.AirportOf(1, 1), "OUT", "8001", "101"),
                (JourneyType.OneWay, OrderFixture.AirportOf(1, 1), OrderFixture.AirportOf(1, 2), "OUT", "8002", "102")
            ],
            _pricing.Requests.Single().PricingUnits.Select(unit => (
                unit.JourneyType,
                unit.PricingOriginAirportId,
                unit.PricingDestinationAirportId,
                unit.BoundIds.Single(),
                unit.AirFares.Single().AirFareId,
                unit.AirFares.Single().Flights.Single().FlightId)));
    }

    [Fact]
    public async Task Pricing_unit_without_a_mapped_semantic_type_is_blocked_before_calling_air_price()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            pricingUnits: [new PricingUnitSpec("SectorSum", FarePricingUnitType.Unspecified, ["OUT"], new FareSpec(8001, 101), new FareSpec(8002, 102))]);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(order, ReservationHarness.AirServices(order)));

        Assert.Equal((2768, 501), (exception.Code, exception.HttpStatus));
        Assert.Empty(_pricing.Requests);
    }

    [Fact]
    public async Task Same_air_fare_in_two_pricing_units_is_validated_in_two_scopes()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            pricingUnits:
            [
                PricingUnitSpec.OneWay("OUT", new FareSpec(7000, 101)),
                PricingUnitSpec.OneWay("IN", new FareSpec(7000, 201))
            ]);

        await ValidateAsync(order, ReservationHarness.AirServices(order));

        Assert.Equal(
            [("OUT", "7000", "101"), ("IN", "7000", "201")],
            _pricing.Requests.Single().PricingUnits.Select(unit => (unit.BoundIds.Single(), unit.AirFares.Single().AirFareId, unit.AirFares.Single().Flights.Single().FlightId)));
    }

    [Fact]
    public async Task Targeted_part_of_a_round_trip_pricing_unit_is_validated_with_the_whole_pricing_unit()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            pricingUnits: [PricingUnitSpec.RoundTripFare(["OUT", "IN"], new FareSpec(9001, 101, 201))]);

        await ValidateAsync(order, [ReservationHarness.AirService(order, 1, 101)]);

        var unit = Assert.Single(_pricing.Requests.Single().PricingUnits);
        Assert.Equal(JourneyType.RoundTrip, unit.JourneyType);
        Assert.Equal(["OUT", "IN"], unit.BoundIds);
        Assert.Equal([("9001", "101"), ("9001", "201")], unit.AirFares.Select(fare => (fare.AirFareId, fare.Flights.Single().FlightId)));
    }

    [Fact]
    public async Task Targeted_part_of_a_one_way_pricing_unit_is_validated_alone()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);

        await ValidateAsync(order, [ReservationHarness.AirService(order, 1, 101)]);

        var request = _pricing.Requests.Single();
        Assert.Single(request.Passengers);
        Assert.Equal(1, request.PricingUnits.Single().AirFares.Single().Flights.Single().RequiredSeats);
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
        Assert.Empty(_pricing.Requests);
    }

    [Fact]
    public async Task Order_created_before_fare_topology_was_preserved_cannot_be_validated()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ((List<OrderFarePricingUnit>)typeof(Order).GetField("_farePricingUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(order)!).Clear();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(order, ReservationHarness.AirServices(order)));

        Assert.Equal(2737, exception.Code);
        Assert.Equal(422, exception.HttpStatus);
        Assert.Empty(_pricing.Requests);
    }

    private Task<DateTimeOffset> ValidateAsync(Order order, IReadOnlyList<OrderAirTransportService> airServices)
        => new AirFareReservationValidator(_pricing, _harness.Clock).ValidateAsync(order, airServices.Select(service => service.Id).ToList());
}
