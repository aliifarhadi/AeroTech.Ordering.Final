using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Providers.Offer.Services;
using AeroTech.Ordering.Providers.Offer.Wire;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class OfferPricingUnitMappingTests
{
    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Captured_pricing_units_keep_their_sequence_kind_bounds_and_components()
    {
        var offer = OfferResponseMapper.ToDomain(CapturedOffer());

        Assert.Equal(
            [
                (1, PricingUnitKind.OneWay, "B1", 1, "B1", 1469435125056929792L, "Y", "YIKAIST/OW", "Flex", "Public"),
                (2, PricingUnitKind.OneWay, "B2", 1, "B2", 1469436464520495104L, "Y", "YISTIKA/OW", "Flex", "Public")
            ],
            offer.PricingUnits.Select(unit =>
            {
                var component = Assert.Single(unit.FareComponents);
                return (unit.Sequence, unit.Kind, unit.CoveredBoundIds.Single(), component.Sequence, component.BoundId, component.AirFareId, component.BookingClass, component.FareBasis, component.FareFamily, component.FareType);
            }));
    }

    [Fact]
    public void Covered_bounds_are_resolved_by_the_published_bound_offer_id()
    {
        var source = CapturedOffer();
        source.AirTransports[0].BoundOfferId = "BO-OUT";
        source.PricingUnits[0].Kind = "ThroughOneWay";
        source.PricingUnits[0].CoveredBoundOfferIds = ["BO-OUT"];

        var unit = OfferResponseMapper.ToDomain(source).PricingUnits[0];

        Assert.Equal(PricingUnitKind.ThroughOneWay, unit.Kind);
        Assert.Equal(["B1"], unit.CoveredBoundIds);
    }

    [Fact]
    public void Bound_without_a_bound_offer_id_is_rejected()
    {
        var source = CapturedOffer();
        source.AirTransports.ForEach(transport => transport.BoundOfferId = string.Empty);

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2758, exception.Code);
    }

    [Fact]
    public void Fare_component_without_a_bound_id_is_rejected()
    {
        var source = CapturedOffer();
        source.PricingUnits[0].FareComponents[0].BoundId = string.Empty;

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2759, exception.Code);
    }

    [Theory]
    [InlineData("RoundTripFromOneWays")]
    [InlineData("SectorSum")]
    [InlineData("2")]
    public void Pricing_unit_kind_outside_the_source_vocabulary_is_rejected(string kind)
    {
        var source = CapturedOffer();
        source.PricingUnits[0].Kind = kind;

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2750, exception.Code);
    }

    [Fact]
    public void Covered_bound_offer_outside_the_offer_is_rejected()
    {
        var source = CapturedOffer();
        source.PricingUnits[0].CoveredBoundOfferIds = ["999"];

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(source));

        Assert.Equal(2751, exception.Code);
    }

    private static FlightOfferDetailResponse CapturedOffer()
        => JsonSerializer.Deserialize<OfferEnvelope<FlightOfferDetailResponse>>(
                   File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Offer", "Payloads", "offer-detail-round-trip-one-way-units.json")),
                   WireOptions)!
               .Data!;
}
