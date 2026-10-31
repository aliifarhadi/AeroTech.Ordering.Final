using AeroTech.Ordering.Providers.Offer.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class OfferBaggageMappingTests
{
    private const string UnsetUnit = "0";

    [Fact]
    public void A_zero_allowance_with_an_unset_unit_means_the_fare_has_no_allowance()
    {
        var source = CapturedOffers.WithBoundIdentity();
        var coupon = source.Tickets[0].Coupons[0];

        (coupon.CabinBaggagePieces, coupon.CabinBaggageWeight, coupon.CabinBaggageUnit) = (0, 0m, UnsetUnit);

        Assert.Null(OfferResponseMapper.ToDomain(source).Tickets[0].Coupons[0].CabinBaggage);
    }

    [Fact]
    public void A_real_allowance_with_an_unset_unit_reaches_the_domain_to_be_refused()
    {
        var source = CapturedOffers.WithBoundIdentity();
        var coupon = source.Tickets[0].Coupons[0];

        (coupon.CabinBaggagePieces, coupon.CabinBaggageWeight, coupon.CabinBaggageUnit) = (1, 7m, UnsetUnit);

        Assert.Equal(UnsetUnit, OfferResponseMapper.ToDomain(source).Tickets[0].Coupons[0].CabinBaggage!.Unit);
    }
}
