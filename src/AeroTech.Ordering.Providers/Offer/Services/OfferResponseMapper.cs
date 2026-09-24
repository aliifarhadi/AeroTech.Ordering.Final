using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Providers.Offer.Services
{
    public static class OfferResponseMapper
    {
        public static OfferDetail ToDomain(Wire.FlightOfferDetailResponse source)
            => new(
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
                MapPricingUnits(source),
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

        private static List<OfferPricingUnit> MapPricingUnits(Wire.FlightOfferDetailResponse source)
        {
            var boundByBoundOfferId = source.AirTransports.ToDictionary(
                transport => string.IsNullOrWhiteSpace(transport.BoundOfferId)
                    ? throw ExceptionFactory.OfferBoundOfferIdIsMissing(transport.BoundId)
                    : transport.BoundOfferId,
                transport => transport.BoundId,
                StringComparer.Ordinal);

            return source.PricingUnits
                .Select((unit, unitIndex) => new OfferPricingUnit(
                    unitIndex + 1,
                    KindOf(unit.Kind),
                    unit.CoveredBoundOfferIds
                        .Select(boundOfferId => boundByBoundOfferId.TryGetValue(boundOfferId, out var boundId)
                            ? boundId
                            : throw ExceptionFactory.OfferCoveredBoundIsNotRecognised(boundOfferId))
                        .ToList(),
                    unit.FareComponents
                        .Select((component, componentIndex) => new OfferFareComponent(
                            componentIndex + 1,
                            string.IsNullOrWhiteSpace(component.BoundId)
                                ? throw ExceptionFactory.OfferFareComponentBoundIsMissing(component.AirFareId)
                                : component.BoundId,
                            component.AirFareId,
                            component.BookingClass,
                            component.FareBasis,
                            component.FareFamily,
                            component.FareType))
                        .ToList()))
                .ToList();
        }

        private static PricingUnitKind KindOf(string kind)
        {
            var parsed = Enum.GetValues<PricingUnitKind>().FirstOrDefault(value => string.Equals(value.ToString(), kind, StringComparison.Ordinal));

            return Enum.IsDefined(parsed) ? parsed : throw ExceptionFactory.OfferPricingUnitKindIsNotRecognised(kind);
        }

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
