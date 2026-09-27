using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.FareTopology;

public sealed class FarePricingAtomDomainTests
{
    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Through_one_way_fare_component_is_one_atom_over_all_of_its_segments()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);

        Assert.Equal([AirServiceIds(order, (1, 101), (1, 102))], AtomsOf(order));
    }

    [Fact]
    public void Each_fare_component_of_a_one_way_pricing_unit_is_its_own_atom()
    {
        var order = Create(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            [PricingUnitSpec.ThroughOneWay("OUT", new FareSpec(8001, 101), new FareSpec(8002, 102))]);

        Assert.Equal([AirServiceIds(order, (1, 101)), AirServiceIds(order, (1, 102))], AtomsOf(order));
    }

    [Fact]
    public void One_way_fare_component_atom_covers_every_traveller_it_prices()
    {
        var order = Create([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);

        Assert.Equal([AirServiceIds(order, (1, 101), (2, 101))], AtomsOf(order));
    }

    [Theory]
    [InlineData(FarePricingUnitType.RoundTrip)]
    [InlineData(FarePricingUnitType.OpenJaw)]
    [InlineData(FarePricingUnitType.CircleTrip)]
    public void Journey_pricing_unit_is_one_atom_over_all_of_its_fare_components(FarePricingUnitType semanticType)
    {
        var order = Create(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)],
            [new PricingUnitSpec($"{semanticType}Fare", semanticType, ["OUT", "RET"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);

        Assert.Equal([AirServiceIds(order, (1, 101), (1, 201))], AtomsOf(order));
    }

    [Theory]
    [InlineData(FarePricingUnitType.Unspecified)]
    [InlineData(FarePricingUnitType.Other)]
    public void Unmapped_pricing_unit_is_one_conservative_atom(FarePricingUnitType semanticType)
    {
        var order = Create(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            [new PricingUnitSpec("SectorSum", semanticType, ["OUT"], new FareSpec(8001, 101), new FareSpec(8002, 102))]);

        Assert.Equal([AirServiceIds(order, (1, 101), (1, 102))], AtomsOf(order));
    }

    [Fact]
    public void Atoms_covering_a_scope_are_only_the_atoms_that_touch_it()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);

        var atoms = order.FarePricingAtomsCovering(AirServiceIds(order, (1, 101)));

        Assert.Equal([AirServiceIds(order, (1, 101))], atoms.Select(atom => atom.AirServiceIds.Order().ToList()));
    }

    [Fact]
    public void Atom_is_fractured_only_while_it_holds_both_active_and_ended_services()
    {
        var order = RoundTrip();
        var atom = order.FarePricingAtomsCovering(AirServiceIds(order, (1, 101))).Single();

        var intact = atom.IsFracturedAmong(ActiveAirServiceIds(order));
        Cancel(order, (1, 201));
        var fractured = atom.IsFracturedAmong(ActiveAirServiceIds(order));
        Cancel(order, (1, 101));
        var ended = atom.IsFracturedAmong(ActiveAirServiceIds(order));

        Assert.Equal((false, true, false), (intact, fractured, ended));
    }

    [Fact]
    public void Fractured_atom_refuses_issue_of_its_remaining_service()
    {
        var order = RoundTrip();
        Cancel(order, (1, 201));

        var exception = Assert.Throws<BusinessException>(() => order.EnsureAcceptedPricingIsIntactFor(AirServiceIds(order, (1, 101))));

        Assert.Equal((2820, 409), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public void Intact_independent_atom_allows_issue_after_another_atom_is_cancelled()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        Cancel(order, (1, 201));

        order.EnsureAcceptedPricingIsIntactFor(AirServiceIds(order, (1, 101)));
    }

    private Order RoundTrip()
        => Create(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)],
            [PricingUnitSpec.RoundTripFare(["OUT", "RET"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);

    private Order Create(IReadOnlyList<TravellerSpec> travellers, IReadOnlyList<BoundSpec> bounds, IReadOnlyList<PricingUnitSpec>? pricingUnits = null)
        => OrderFixture.Create(_ids, _clock, travellers, bounds, pricingUnits: pricingUnits);

    private void Cancel(Order order, (int Traveller, long Flight) service)
        => order.Cancel(AirServiceIds(order, service), ReservationHarness.CancellingActor, nameof(VoidReason.CustomerRequest), null, _ids, _clock.Now);

    private static List<List<long>> AtomsOf(Order order)
        => order.FarePricingUnits.Single().PricingAtoms().Select(atom => atom.AirServiceIds.Order().ToList()).ToList();

    private static List<long> AirServiceIds(Order order, params (int Traveller, long Flight)[] services)
        => services.Select(service => ReservationHarness.AirService(order, service.Traveller, service.Flight).Id).Order().ToList();

    private static HashSet<long> ActiveAirServiceIds(Order order)
        => order.TicketableAirServices().Select(service => service.Id).ToHashSet();
}
