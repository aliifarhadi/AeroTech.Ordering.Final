using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.Providers.Offer
{
    public sealed class OfferReader
    {
        private readonly OfferDetail _offer;

        public OfferReader(OfferDetail offer) => _offer = offer;

        public string OfferId => _offer.OfferId;

        public int CurrencyId => _offer.CurrencyId;

        public DateTimeOffset? LastTicketingDate => _offer.LastTicketingDate;

        public IReadOnlyList<OfferTraveller> Travellers => _offer.Travellers;

        public IReadOnlyList<OfferBound> BoundsInSequence()
            => _offer.Bounds.OrderBy(bound => bound.Sequence).ToList();

        public IReadOnlyList<OfferFlight> BoundFlightsInOrder(OfferBound bound)
            => bound.Flights.OrderBy(flight => flight.Sequence).ThenBy(flight => flight.DepartureDateTime).ToList();

        public OfferTraveller Traveller(int travellerIndex)
            => _offer.Travellers.FirstOrDefault(traveller => traveller.TravellerIndex == travellerIndex)
               ?? throw ExceptionFactory.OfferHasNoTravellerWithIndex(travellerIndex);

        public string GetTravellerRef(int travellerIndex) => Traveller(travellerIndex).TravellerRef;

        public OfferTicket Ticket(string travellerRef)
            => _offer.Tickets.FirstOrDefault(ticket => SameRef(ticket.TravellerRef, travellerRef))
               ?? throw ExceptionFactory.OfferHasNoTicketForTraveller(travellerRef);

        public OfferCoupon Coupon(string travellerRef, long flightId)
            => Ticket(travellerRef).Coupons.FirstOrDefault(coupon => coupon.FlightId == flightId)
               ?? throw ExceptionFactory.OfferHasNoCouponForFlight(travellerRef, flightId);

        public IReadOnlyList<OfferCoupon> BoundCoupons(string travellerRef, string boundId)
            => Ticket(travellerRef).Coupons.Where(coupon => SameRef(coupon.BoundId, boundId)).ToList();

        public OfferFareComponent? FareComponent(string boundId, long airFareId)
            => _offer.FareComponents.FirstOrDefault(component => SameRef(component.BoundId, boundId) && component.AirFareId == airFareId)
               ?? _offer.FareComponents.FirstOrDefault(component => SameRef(component.BoundId, boundId));

        public IReadOnlyList<OfferPriceLine> OrderChargeLines() => _offer.OrderCharges;

        public long ResolveCouponAirFareId(string travellerRef, string boundId, long flightId)
        {
            var reference = Coupon(travellerRef, flightId).PriceLines
                .Where(line => line.Category == OfferPriceCategory.Fare)
                .Select(line => long.TryParse(line.Reference, out var parsed) ? parsed : 0)
                .FirstOrDefault(id => id > 0);

            if (reference > 0)
                return reference;

            var fareComponent = _offer.FareComponents.FirstOrDefault(component => SameRef(component.BoundId, boundId));
            if (fareComponent is not null && fareComponent.AirFareId > 0)
                return fareComponent.AirFareId;

            throw ExceptionFactory.CouldNotResolveAirFareForBound(boundId);
        }

        public OfferRate? Rate(string? rateOfExchangePeriodId)
            => string.IsNullOrWhiteSpace(rateOfExchangePeriodId)
                ? null
                : _offer.Rates.FirstOrDefault(rate => string.Equals(rate.RateOfExchangePeriodId, rateOfExchangePeriodId, StringComparison.Ordinal));

        public int SourceCurrencyId(OfferPriceLine line)
        {
            if (line.CurrencyId > 0)
                return line.CurrencyId;

            var rate = Rate(line.RateOfExchangePeriodId);
            return rate is not null && rate.FromCurrencyId > 0 ? rate.FromCurrencyId : _offer.CurrencyId;
        }

        public int EquivalentCurrencyId(OfferPriceLine line)
        {
            if (line.EquivalentCurrencyId > 0)
                return line.EquivalentCurrencyId;

            var rate = Rate(line.RateOfExchangePeriodId);
            return rate is not null && rate.ToCurrencyId > 0 ? rate.ToCurrencyId : _offer.CurrencyId;
        }

        private static bool SameRef(string? left, string? right)
            => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
