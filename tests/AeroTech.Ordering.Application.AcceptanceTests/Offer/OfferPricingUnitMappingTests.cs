using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Providers.Offer.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class OfferPricingUnitMappingTests
{
    [Fact]
    public void Captured_pricing_units_keep_their_sequence_kind_bounds_and_components()
    {
        var offer = OfferResponseMapper.ToDomain(CapturedOffers.WithBoundIdentity());

        Assert.Equal(
            [
                (1, "OneWay", FarePricingUnitType.OneWay, "B1", 1, "B1", 1469435125056929792L, "Y", "YIKAIST/OW", "Flex", "Public"),
                (2, "OneWay", FarePricingUnitType.OneWay, "B2", 1, "B2", 1469436464520495104L, "Y", "YISTIKA/OW", "Flex", "Public")
            ],
            offer.PricingUnits.Select(unit =>
            {
                var component = Assert.Single(unit.FareComponents);
                return (unit.Sequence, unit.SourceKind, unit.SemanticType, unit.CoveredBoundIds.Single(), component.Sequence, component.BoundId, component.AirFareId, component.BookingClass, component.FareBasis, component.FareFamily, component.FareType);
            }));
    }

    [Fact]
    public void Offer_captured_before_bound_identity_stays_verbatim_and_is_rejected()
    {
        var raw = CapturedOffers.RawBeforeBoundIdentity();

        var exception = Assert.Throws<BusinessException>(() => OfferResponseMapper.ToDomain(CapturedOffers.Parse(raw)));

        Assert.DoesNotContain("boundOfferId", raw);
        Assert.Equal(2758, exception.Code);
    }
}
