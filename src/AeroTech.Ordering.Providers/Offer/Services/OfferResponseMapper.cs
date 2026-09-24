using AeroTech.Ordering.Domain.Providers.Offer;

namespace AeroTech.Ordering.Providers.Offer.Services
{
    public static class OfferResponseMapper
    {
        public static OfferDetail ToDomain(Wire.FlightOfferDetailResponse source)
        {
            var boundByFlight = source.AirTransports
                .SelectMany(transport => transport.Flights.Select(flight => (transport.BoundId, flight.FlightId)))
                .ToDictionary(pair => pair.FlightId, pair => pair.BoundId);

            return new OfferDetail(
                source.OfferId,
                source.CurrencyId,
                source.LastTicketingDate,
                source.Tickets
                    .Select(ticket => new OfferTraveller(ticket.TravellerRef, ticket.TravellerIndex, ticket.PassengerTypeCode))
                    .ToList(),
                source.AirTransports
                    .Select(transport => new OfferBound(
                        transport.BoundId,
                        transport.Sequence,
                        transport.OriginAirportId,
                        transport.DestinationAirportId,
                        transport.Flights.Select(MapFlight).ToList()))
                    .ToList(),
                MapFareComponents(source, boundByFlight),
                source.Tickets
                    .Select(ticket => new OfferTicket(
                        ticket.TravellerRef,
                        ticket.TravellerIndex,
                        ticket.Coupons.Select(MapCoupon).ToList()))
                    .ToList(),
                source.OrderCharges.Select(MapPriceLine).ToList(),
                source.RatesOfExchange
                    .Select(rate => new OfferRate(
                        rate.RateOfExchangePeriodId,
                        rate.FromCurrencyId,
                        rate.ToCurrencyId,
                        rate.Rate,
                        rate.DecimalPlaces))
                    .ToList());
        }

        private static List<OfferFareComponent> MapFareComponents(
            Wire.FlightOfferDetailResponse source,
            IReadOnlyDictionary<long, string> boundByFlight)
            => source.PricingUnits
                .SelectMany(unit => unit.FareComponents)
                .SelectMany(fareComponent => BoundsPricedBy(source, boundByFlight, fareComponent.AirFareId)
                    .Select(boundId => new OfferFareComponent(
                        fareComponent.AirFareId,
                        boundId,
                        fareComponent.BookingClass,
                        fareComponent.FareBasis,
                        fareComponent.FareFamily,
                        fareComponent.FareType)))
                .DistinctBy(fareComponent => (fareComponent.AirFareId, fareComponent.BoundId))
                .ToList();

        private static IEnumerable<string> BoundsPricedBy(
            Wire.FlightOfferDetailResponse source,
            IReadOnlyDictionary<long, string> boundByFlight,
            long airFareId)
            => source.Tickets
                .SelectMany(ticket => ticket.Coupons)
                .Where(coupon => coupon.Pricings.Any(line =>
                    line.Category == Wire.OfferPricingCategory.Fare
                    && line.Reference == airFareId.ToString()))
                .Select(coupon => boundByFlight.TryGetValue(coupon.FlightId, out var boundId) ? boundId : coupon.BoundId)
                .Distinct(StringComparer.Ordinal);

        private static OfferCoupon MapCoupon(Wire.OfferCoupon coupon)
            => new(
                coupon.BoundId,
                coupon.FlightId,
                coupon.IsRefundable,
                coupon.IsChangeable,
                coupon.IsUpgradable,
                MapBaggage(coupon.BaggagePieces, coupon.BaggageWeight, coupon.BaggageUnit),
                MapBaggage(coupon.CabinBaggagePieces, coupon.CabinBaggageWeight, coupon.CabinBaggageUnit),
                coupon.Pricings.Select(MapPriceLine).ToList());

        private static OfferBaggage? MapBaggage(int pieces, decimal weight, string? unit)
            => string.IsNullOrWhiteSpace(unit) ? null : new OfferBaggage(pieces, weight, unit);

        private static OfferFlight MapFlight(Wire.OfferFlight flight)
            => new(
                flight.Sequence,
                flight.FlightId,
                flight.FlightVersion,
                flight.FlightNumber,
                flight.OriginAirportId,
                flight.OriginAirportTerminalId,
                flight.DestinationAirportId,
                flight.DestinationAirportTerminalId,
                flight.OperatingAirlineId,
                flight.MarketingAirlineId,
                flight.DepartureDateTime,
                flight.ArrivalDateTime,
                flight.Duration,
                flight.AircraftId,
                flight.RbdId,
                flight.FlightCapacityId,
                flight.Legs
                    .Select(leg => new OfferFlightLeg(
                        leg.Sequence,
                        leg.LegId,
                        leg.OriginAirportId,
                        leg.OriginAirportTerminalId,
                        leg.DestinationAirportId,
                        leg.DestinationAirportTerminalId,
                        leg.DepartureDateTime,
                        leg.ArrivalDateTime,
                        MapStop(leg.Stop)))
                    .ToList());

        private static OfferFlightStop? MapStop(Wire.OfferFlightStop? stop)
            => stop is null
                ? null
                : new OfferFlightStop(stop.DurationMinutes, stop.StopType, stop.PassengersCanBoardOrLeave);

        private static OfferPriceLine MapPriceLine(Wire.OfferPricingLine line)
            => new(
                (OfferPriceCategory)line.Category,
                line.Name,
                line.Code,
                line.Reference,
                line.Amount,
                line.CurrencyId,
                line.EquivalentAmount,
                line.EquivalentCurrencyId,
                line.RateOfExchangePeriodId);
    }
}
