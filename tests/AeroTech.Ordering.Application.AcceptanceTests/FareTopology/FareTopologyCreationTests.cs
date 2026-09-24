using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.FareTopology;

public sealed class FareTopologyCreationTests
{
    private readonly SequentialIdGenerator _ids = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public void Round_trip_pricing_unit_keeps_both_components_in_one_pricing_unit()
    {
        var order = Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            [PricingUnitSpec.RoundTripFare(["OUT", "IN"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);

        var pricingUnit = Assert.Single(order.FarePricingUnits);

        Assert.Equal(
            (1, "RoundTripFare", FarePricingUnitType.RoundTrip, order.Changes.Single().Id),
            (pricingUnit.Sequence, pricingUnit.SourceKind, pricingUnit.SemanticType, pricingUnit.CreatedByChangeId));
        Assert.Equal([JourneyId(order, "OUT"), JourneyId(order, "IN")], pricingUnit.CoveredJourneyIds);
        Assert.Equal(
            [(1, 9001L, AirServiceIds(order, 101)), (2, 9002L, AirServiceIds(order, 201))],
            Components(pricingUnit));
    }

    [Fact]
    public void Two_one_way_pricing_units_stay_two_pricing_units()
    {
        var order = Create([new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);

        Assert.Equal(
            [
                (1, "OneWay", FarePricingUnitType.OneWay, JourneyId(order, "OUT"), 7001L, AirServiceIds(order, 101)),
                (2, "OneWay", FarePricingUnitType.OneWay, JourneyId(order, "IN"), 7002L, AirServiceIds(order, 201))
            ],
            order.FarePricingUnits.Select(unit =>
            {
                var component = Assert.Single(unit.FareComponents);
                return (unit.Sequence, unit.SourceKind, unit.SemanticType, unit.CoveredJourneyIds.Single(), component.AirFareId, Ids(component.CoveredOrderServiceIds));
            }));
    }

    [Fact]
    public void Connecting_itinerary_is_one_component_covering_both_segments()
    {
        var order = Create([new BoundSpec("OUT", 101, 102)]);

        var pricingUnit = Assert.Single(order.FarePricingUnits);
        var component = Assert.Single(pricingUnit.FareComponents);

        Assert.Equal(("ThroughOneWay", FarePricingUnitType.OneWay), (pricingUnit.SourceKind, pricingUnit.SemanticType));
        Assert.Equal(AirServiceIds(order, 101, 102), Ids(component.CoveredOrderServiceIds));
    }

    [Fact]
    public void Source_kind_without_a_mapped_meaning_is_preserved_verbatim_with_its_topology()
    {
        var order = Create(
            [new BoundSpec("OUT", 101, 102)],
            [new PricingUnitSpec("SectorSum", FarePricingUnitType.Unspecified, ["OUT"], new FareSpec(8001, 101), new FareSpec(8002, 102))]);

        var pricingUnit = Assert.Single(order.FarePricingUnits);

        Assert.Equal(("SectorSum", FarePricingUnitType.Unspecified), (pricingUnit.SourceKind, pricingUnit.SemanticType));
        Assert.Equal([JourneyId(order, "OUT")], pricingUnit.CoveredJourneyIds);
        Assert.Equal(
            [(1, 8001L, AirServiceIds(order, 101)), (2, 8002L, AirServiceIds(order, 102))],
            Components(pricingUnit));
    }

    [Fact]
    public void Source_one_way_kind_over_a_connecting_bound_is_preserved()
    {
        var order = Create(
            [new BoundSpec("OUT", 101, 102)],
            [PricingUnitSpec.OneWay("OUT", new FareSpec(7001, 101, 102))]);

        var pricingUnit = Assert.Single(order.FarePricingUnits);

        Assert.Equal(("OneWay", FarePricingUnitType.OneWay), (pricingUnit.SourceKind, pricingUnit.SemanticType));
        Assert.Equal(AirServiceIds(order, 101, 102), Ids(Assert.Single(pricingUnit.FareComponents).CoveredOrderServiceIds));
    }

    [Fact]
    public void Coverage_follows_each_coupon_fare_reference()
    {
        var order = Create(
            [new BoundSpec("OUT", 101)],
            [
                PricingUnitSpec.OneWay("OUT", new FareSpec(7001, 101) { TravellerIndexes = [1] }),
                PricingUnitSpec.OneWay("OUT", new FareSpec(7101, 101) { TravellerIndexes = [2] })
            ]);

        Assert.Equal(
            [(7001L, AirServiceOf(order, 1, 101)), (7101L, AirServiceOf(order, 2, 101))],
            order.FarePricingUnits.Select(unit =>
            {
                var component = Assert.Single(unit.FareComponents);
                return (component.AirFareId, Ids(component.CoveredOrderServiceIds));
            }));
    }

    [Fact]
    public void Pricing_unit_whose_components_cover_other_bounds_than_the_source_lists_is_rejected()
    {
        var exception = Assert.Throws<BusinessException>(() => Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            [
                PricingUnitSpec.RoundTripFare(["OUT", "IN"], new FareSpec(9001, 101)),
                PricingUnitSpec.OneWay("IN", new FareSpec(7002, 201))
            ]));

        Assert.Equal(2760, exception.Code);
    }

    [Fact]
    public void Same_air_fare_in_two_pricing_units_remains_two_occurrences()
    {
        var order = Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            [
                PricingUnitSpec.OneWay("OUT", new FareSpec(7000, 101)),
                PricingUnitSpec.OneWay("IN", new FareSpec(7000, 201))
            ]);

        var components = order.FarePricingUnits.SelectMany(unit => unit.FareComponents).ToList();

        Assert.Equal(2, components.Count);
        Assert.All(components, component => Assert.Equal(7000, component.AirFareId));
        Assert.NotEqual(components[0].Id, components[1].Id);
        Assert.Equal(AirServiceIds(order, 101), Ids(components[0].CoveredOrderServiceIds));
        Assert.Equal(AirServiceIds(order, 201), Ids(components[1].CoveredOrderServiceIds));
    }

    [Fact]
    public void Round_trip_fare_occurrences_are_told_apart_by_their_bound()
    {
        var order = Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            [PricingUnitSpec.RoundTripFare(["OUT", "IN"], new FareSpec(9001, 101, 201))]);

        var pricingUnit = Assert.Single(order.FarePricingUnits);

        Assert.Equal(
            [(1, 9001L, AirServiceIds(order, 101)), (2, 9001L, AirServiceIds(order, 201))],
            Components(pricingUnit));
    }

    [Fact]
    public void Same_air_fare_twice_on_one_bound_of_a_pricing_unit_is_blocked_by_the_offer_contract()
    {
        var exception = Assert.Throws<BusinessException>(() => Create(
            [new BoundSpec("OUT", 101, 102)],
            [PricingUnitSpec.ThroughOneWay("OUT", new FareSpec(9001, 101), new FareSpec(9001, 102))]));

        Assert.Equal(2753, exception.Code);
        Assert.Equal(501, exception.HttpStatus);
    }

    [Fact]
    public void Fare_component_on_a_bound_its_pricing_unit_does_not_cover_is_rejected()
    {
        var exception = Assert.Throws<BusinessException>(() => Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)],
            [PricingUnitSpec.OneWay("OUT", new FareSpec(7001, 101), new FareSpec(7002, 201))]));

        Assert.Equal(2757, exception.Code);
    }

    [Fact]
    public void Fare_component_that_prices_no_coupon_is_rejected()
    {
        var exception = Assert.Throws<BusinessException>(() => Create(
            [new BoundSpec("OUT", 101)],
            [PricingUnitSpec.OneWay("OUT", new FareSpec(7001, 101), new FareSpec(7099))]));

        Assert.Equal(2754, exception.Code);
    }

    [Fact]
    public void Pricing_unit_covering_a_bound_outside_the_order_is_rejected()
    {
        var exception = Assert.Throws<BusinessException>(() => Create(
            [new BoundSpec("OUT", 101)],
            [PricingUnitSpec.OneWay("ELSEWHERE", new FareSpec(7001, 101))]));

        Assert.Equal(2752, exception.Code);
    }

    [Fact]
    public void Pricing_reconciliation_is_unchanged_and_topology_carries_no_amounts()
    {
        var order = Create(
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201, 202)],
            [
                PricingUnitSpec.OneWay("OUT", new FareSpec(7001, 101)),
                PricingUnitSpec.ThroughOneWay("IN", new FareSpec(7002, 201, 202))
            ]);

        Assert.Equal(600m, order.CustomerTotal);
        Assert.Equal(6, order.PricingLines.Count);
        Assert.Equal([100m, 100m, 200m, 200m], order.Items.Select(item => item.AcceptedTotal).Order());
        Assert.All(order.PricingLines, line => Assert.Equal(line.Amount, line.Allocations.Sum(allocation => allocation.Amount)));
        Assert.DoesNotContain(
            typeof(OrderFarePricingUnit).GetProperties().Concat(typeof(OrderFareComponent).GetProperties()),
            property => property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?));
    }

    private Order Create(IReadOnlyList<BoundSpec> bounds, IReadOnlyList<PricingUnitSpec>? pricingUnits = null)
        => OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1), TravellerSpec.Adult(2)], bounds, pricingUnits: pricingUnits);

    private static long JourneyId(Order order, string boundId)
        => order.Journeys.Single(journey => journey.BoundId == boundId).Id;

    private static string AirServiceIds(Order order, params long[] flightIds)
    {
        var segmentIds = order.Segments
            .Where(segment => flightIds.Contains(segment.FlightId))
            .Select(segment => segment.Id)
            .ToHashSet();

        return Ids(order.Services
            .OfType<OrderAirTransportService>()
            .Where(service => segmentIds.Contains(service.SegmentId))
            .Select(service => service.Id));
    }

    private static string AirServiceOf(Order order, int travellerIndex, long flightId)
    {
        var travellerId = order.Travellers.Single(traveller => traveller.Index == travellerIndex).Id;
        var segmentId = order.Segments.Single(segment => segment.FlightId == flightId).Id;

        return Ids(order.Services
            .OfType<OrderAirTransportService>()
            .Where(service => service.TravellerId == travellerId && service.SegmentId == segmentId)
            .Select(service => service.Id));
    }

    private static string Ids(IEnumerable<long> ids) => string.Join(",", ids.Order());

    private static List<(int Sequence, long AirFareId, string CoveredOrderServiceIds)> Components(OrderFarePricingUnit pricingUnit)
        => pricingUnit.FareComponents
            .Select(component => (component.Sequence, component.AirFareId, Ids(component.CoveredOrderServiceIds)))
            .ToList();
}
