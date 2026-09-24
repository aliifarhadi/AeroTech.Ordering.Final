using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Providers.Offer.Services;
using AeroTech.Ordering.Providers.Offer.Wire;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class SyntheticOfferPricingUnitContractTests
{
    [Fact]
    public void Synthetic_round_trip_fare_occurrences_keep_their_kind_and_bounds()
    {
        var source = CapturedOffers.WithBoundIdentity();
        var outbound = source.AirTransports[0];
        var inbound = source.AirTransports[1];
        var fare = source.PricingUnits[0].FareComponents[0];
        source.PricingUnits =
        [
            new OfferPricingUnit
            {
                Kind = "RoundTripFare",
                CoveredBoundOfferIds = [outbound.BoundOfferId, inbound.BoundOfferId],
                FareComponents = [OccurrenceOf(fare, fare.AirFareId, outbound.BoundId), OccurrenceOf(fare, fare.AirFareId, inbound.BoundId)]
            }
        ];

        var unit = Assert.Single(OfferResponseMapper.ToDomain(source).PricingUnits);

        Assert.Equal(PricingUnitKind.RoundTripFare, unit.Kind);
        Assert.Equal([outbound.BoundId, inbound.BoundId], unit.CoveredBoundIds);
        Assert.Equal(
            [(1, outbound.BoundId, fare.AirFareId), (2, inbound.BoundId, fare.AirFareId)],
            unit.FareComponents.Select(component => (component.Sequence, component.BoundId, component.AirFareId)));
    }

    [Fact]
    public void Synthetic_sector_sum_keeps_one_fare_component_per_sector_of_the_bound()
    {
        var source = CapturedOffers.WithBoundIdentity();
        var outbound = source.AirTransports[0];
        var fare = source.PricingUnits[0].FareComponents[0];
        const long secondSectorFareId = 9001;
        source.PricingUnits[0] = new OfferPricingUnit
        {
            Kind = "SectorSum",
            CoveredBoundOfferIds = [outbound.BoundOfferId],
            FareComponents = [OccurrenceOf(fare, fare.AirFareId, outbound.BoundId), OccurrenceOf(fare, secondSectorFareId, outbound.BoundId)]
        };

        var unit = OfferResponseMapper.ToDomain(source).PricingUnits[0];

        Assert.Equal(PricingUnitKind.SectorSum, unit.Kind);
        Assert.Equal([outbound.BoundId], unit.CoveredBoundIds);
        Assert.Equal(
            [(1, outbound.BoundId, fare.AirFareId), (2, outbound.BoundId, secondSectorFareId)],
            unit.FareComponents.Select(component => (component.Sequence, component.BoundId, component.AirFareId)));
    }

    [Fact]
    public void Synthetic_covered_bounds_are_resolved_by_the_published_bound_offer_id()
    {
        var source = CapturedOffers.WithBoundIdentity();
        source.AirTransports[0].BoundOfferId = "BO-OUT";
        source.PricingUnits[0].Kind = "ThroughOneWay";
        source.PricingUnits[0].CoveredBoundOfferIds = ["BO-OUT"];

        var unit = OfferResponseMapper.ToDomain(source).PricingUnits[0];

        Assert.Equal(PricingUnitKind.ThroughOneWay, unit.Kind);
        Assert.Equal(["B1"], unit.CoveredBoundIds);
    }

    [Fact]
    public void Synthetic_bound_without_a_bound_offer_id_is_rejected()
    {
        var source = CapturedOffers.WithBoundIdentity();
        source.AirTransports.ForEach(transport => transport.BoundOfferId = string.Empty);

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2758, exception.Code);
    }

    [Fact]
    public void Synthetic_fare_component_without_a_bound_id_is_rejected()
    {
        var source = CapturedOffers.WithBoundIdentity();
        source.PricingUnits[0].FareComponents[0].BoundId = string.Empty;

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2759, exception.Code);
    }

    [Theory]
    [InlineData("RoundTripFromOneWays")]
    [InlineData("Sector Sum")]
    [InlineData("2")]
    public void Synthetic_pricing_unit_kind_outside_the_source_vocabulary_is_rejected(string kind)
    {
        var source = CapturedOffers.WithBoundIdentity();
        source.PricingUnits[0].Kind = kind;

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2750, exception.Code);
    }

    [Fact]
    public void Synthetic_covered_bound_offer_outside_the_offer_is_rejected()
    {
        var source = CapturedOffers.WithBoundIdentity();
        source.PricingUnits[0].CoveredBoundOfferIds = ["999"];

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2751, exception.Code);
    }

    private static OfferFareComponent OccurrenceOf(OfferFareComponent fare, long airFareId, string boundId)
        => new()
        {
            AirFareId = airFareId,
            BoundId = boundId,
            BookingClass = fare.BookingClass,
            FareBasis = fare.FareBasis,
            FareFamily = fare.FareFamily,
            FareType = fare.FareType
        };
}
