using System.Globalization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Pricing;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers._Shared;
using AirPriceJourneyType = AeroTech.Messages.AirPrice.Enums.JourneyType;

namespace AeroTech.Ordering.Providers.Pricing.Services
{
    public sealed class AirFareReservationValidator : IAirFareReservationValidator
    {
        private const string ProviderName = "AirPrice";

        private readonly IPricingProvider _pricing;
        private readonly IClock _clock;

        public AirFareReservationValidator(IPricingProvider pricing, IClock clock)
        {
            _pricing = pricing;
            _clock = clock;
        }

        public async Task<DateTimeOffset> ValidateAsync(
            Order order,
            IReadOnlyCollection<long> airServiceIds,
            CancellationToken cancellationToken = default)
        {
            var result = await _pricing.ReservationValidationAsync(RequestFor(order, airServiceIds), cancellationToken);

            return result.TimeLimit;
        }

        private AirFareBoundReservationValidationRequest RequestFor(Order order, IReadOnlyCollection<long> airServiceIds)
        {
            var travellers = order.Travellers.ToDictionary(traveller => traveller.Id);
            var segments = order.Segments.ToDictionary(segment => segment.Id);
            var journeys = order.Journeys.ToDictionary(journey => journey.Id);

            var airServices = order.Services
                .OfType<OrderAirTransportService>()
                .Where(service => service.CommercialStatus == OrderServiceCommercialState.Active)
                .ToList();

            var pricedServices = airServices
                .Where(service => airServiceIds.Contains(service.Id))
                .Select(service => Priced(service, segments[service.SegmentId], travellers[service.TravellerId]))
                .ToList();

            var fares = pricedServices
                .Select(priced => priced.AirFareId)
                .Distinct()
                .ToDictionary(airFareId => airFareId, airFareId => FareOf(airFareId, airServices, segments, journeys));

            return new AirFareBoundReservationValidationRequest(
                new AirFareBoundReservationSalesContext(
                    order.SalesContext.ActorId,
                    null,
                    order.SalesContext.Channel,
                    order.CustomerId,
                    _clock.GetDateTime(),
                    order.CurrencyId),
                pricedServices
                    .Select(priced => priced.Traveller)
                    .DistinctBy(traveller => traveller.Id)
                    .Select(traveller => new AirFareBoundReservationPassenger(
                        traveller.Id.ToString(CultureInfo.InvariantCulture),
                        AirPricePassengerTypes.From(traveller.PassengerType, ProviderName)))
                    .ToList(),
                pricedServices
                    .GroupBy(priced => fares[priced.AirFareId].PricingUnit)
                    .Select(pricingUnit => new AirFareBoundReservationPricingUnit(
                        $"{pricingUnit.Key.JourneyType}:{pricingUnit.Key.OriginAirportId}-{pricingUnit.Key.DestinationAirportId}",
                        pricingUnit.Key.JourneyType,
                        pricingUnit.SelectMany(priced => fares[priced.AirFareId].BoundIds).Distinct(StringComparer.Ordinal).ToList(),
                        pricingUnit.Key.OriginAirportId,
                        pricingUnit.Key.DestinationAirportId,
                        pricingUnit
                            .GroupBy(priced => priced.AirFareId)
                            .Select(fare => new AirFareBoundReservationAirFare(
                                fare.Key.ToString(CultureInfo.InvariantCulture),
                                fare.GroupBy(priced => priced.Segment.Id).Select(FlightOf).ToList()))
                            .ToList()))
                    .ToList());
        }

        private static Fare FareOf(
            long airFareId,
            IReadOnlyList<OrderAirTransportService> airServices,
            IReadOnlyDictionary<long, OrderSegment> segments,
            IReadOnlyDictionary<long, OrderJourney> journeys)
        {
            var fareSegments = airServices
                .Where(service => service.AirFareId == airFareId)
                .Select(service => segments[service.SegmentId])
                .DistinctBy(segment => segment.Id)
                .ToList();

            var fareJourneys = fareSegments
                .Select(segment => journeys[segment.OrderJourneyId])
                .DistinctBy(journey => journey.Id)
                .OrderBy(journey => journey.Sequence)
                .ToList();

            var pricingSegments = fareSegments
                .Where(segment => segment.OrderJourneyId == fareJourneys[0].Id)
                .OrderBy(segment => segment.Sequence)
                .ToList();

            return new Fare(
                new PricingUnit(
                    fareJourneys.Count > 1 ? AirPriceJourneyType.RoundTrip : AirPriceJourneyType.OneWay,
                    pricingSegments[0].OriginAirportId,
                    pricingSegments[^1].DestinationAirportId),
                fareJourneys.Select(journey => journey.BoundId).ToList());
        }

        private static AirFareBoundReservationFlight FlightOf(IGrouping<long, PricedService> flight)
        {
            var priced = flight.First();

            return new AirFareBoundReservationFlight(
                priced.Segment.FlightId.ToString(CultureInfo.InvariantCulture),
                priced.FlightNumber,
                priced.Segment.OriginAirportId,
                priced.Segment.DestinationAirportId,
                priced.AircraftId,
                priced.Segment.SoldDeparture,
                FlightStatus.Open,
                null,
                priced.RbdId,
                flight.Count(service => service.Traveller.InfantParentTravellerId is null));
        }

        private static PricedService Priced(OrderAirTransportService service, OrderSegment segment, OrderTraveller traveller)
            => service is { AirFareId: { } airFareId, RbdId: { } rbdId }
               && segment is { FlightNumber: { } flightNumber, AircraftId: { } aircraftId }
                ? new PricedService(segment, traveller, airFareId, rbdId, flightNumber, aircraftId)
                : throw ExceptionFactory.AirServiceValidationFactsAreMissing(service.Id);

        private sealed record PricingUnit(AirPriceJourneyType JourneyType, int OriginAirportId, int DestinationAirportId);

        private sealed record Fare(PricingUnit PricingUnit, IReadOnlyList<string> BoundIds);

        private sealed record PricedService(
            OrderSegment Segment,
            OrderTraveller Traveller,
            long AirFareId,
            long RbdId,
            string FlightNumber,
            int AircraftId);
    }
}
