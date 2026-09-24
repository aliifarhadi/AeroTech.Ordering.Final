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
            var itinerary = new Itinerary(order);
            var scope = airServiceIds.ToHashSet();

            var coveredServiceIds = order.FarePricingUnits
                .SelectMany(pricingUnit => pricingUnit.FareComponents)
                .SelectMany(component => component.CoveredOrderServiceIds)
                .ToHashSet();

            if (scope.Except(coveredServiceIds).ToList() is [var uncoveredServiceId, ..])
                throw ExceptionFactory.AirServiceValidationFactsAreMissing(uncoveredServiceId);

            var pricingUnits = order.FarePricingUnits
                .OrderBy(pricingUnit => pricingUnit.Sequence)
                .Where(pricingUnit => pricingUnit.FareComponents.Any(component => component.CoveredOrderServiceIds.Any(scope.Contains)))
                .Select(pricingUnit => new ScopedPricingUnit(
                    pricingUnit,
                    pricingUnit.FareComponents
                        .OrderBy(component => component.Sequence)
                        .Select(component => new ScopedFareComponent(
                            component,
                            component.CoveredOrderServiceIds
                                .Where(serviceId => IsIndivisible(pricingUnit) || scope.Contains(serviceId))
                                .Select(itinerary.Priced)
                                .ToList()))
                        .Where(component => component.Services.Count > 0)
                        .ToList()))
                .ToList();

            return new AirFareBoundReservationValidationRequest(
                new AirFareBoundReservationSalesContext(
                    order.SalesContext.ActorId,
                    null,
                    order.SalesContext.Channel,
                    order.CustomerId,
                    _clock.GetDateTime(),
                    order.CurrencyId),
                pricingUnits
                    .SelectMany(pricingUnit => pricingUnit.FareComponents)
                    .SelectMany(component => component.Services)
                    .Select(priced => priced.Traveller)
                    .DistinctBy(traveller => traveller.Id)
                    .Select(traveller => new AirFareBoundReservationPassenger(
                        traveller.Id.ToString(CultureInfo.InvariantCulture),
                        AirPricePassengerTypes.From(traveller.PassengerType, ProviderName)))
                    .ToList(),
                pricingUnits.SelectMany(pricingUnit => PricingUnitsOf(pricingUnit, itinerary)).ToList());
        }

        private static IEnumerable<AirFareBoundReservationPricingUnit> PricingUnitsOf(ScopedPricingUnit scoped, Itinerary itinerary)
            => scoped.PricingUnit.Kind == PricingUnitKind.SectorSum
                ? scoped.FareComponents.Select(fare => PricingUnitOf(scoped.PricingUnit, [fare], itinerary))
                : [PricingUnitOf(scoped.PricingUnit, scoped.FareComponents, itinerary)];

        private static AirFareBoundReservationPricingUnit PricingUnitOf(
            OrderFarePricingUnit pricingUnit,
            IReadOnlyList<ScopedFareComponent> fares,
            Itinerary itinerary)
        {
            var journeys = pricingUnit.CoveredJourneyIds
                .Select(itinerary.Journey)
                .OrderBy(journey => journey.Sequence)
                .ToList();

            var coveredSegments = fares
                .SelectMany(fare => fare.Component.CoveredOrderServiceIds)
                .Select(itinerary.SegmentOf)
                .DistinctBy(segment => segment.Id)
                .OrderBy(segment => itinerary.Journey(segment.OrderJourneyId).Sequence)
                .ThenBy(segment => segment.Sequence)
                .ToList();

            var pricingSegments = coveredSegments
                .Where(segment => segment.OrderJourneyId == coveredSegments[0].OrderJourneyId)
                .ToList();

            return new AirFareBoundReservationPricingUnit(
                pricingUnit.Id.ToString(CultureInfo.InvariantCulture),
                JourneyTypeOf(pricingUnit.Kind),
                journeys.Select(journey => journey.BoundId).ToList(),
                pricingSegments[0].OriginAirportId,
                pricingSegments[^1].DestinationAirportId,
                fares
                    .Select(fare => new AirFareBoundReservationAirFare(
                        fare.Component.AirFareId.ToString(CultureInfo.InvariantCulture),
                        fare.Services
                            .GroupBy(priced => priced.Segment.Id)
                            .Select(FlightOf)
                            .ToList()))
                    .ToList());
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

        private static bool IsIndivisible(OrderFarePricingUnit pricingUnit)
            => pricingUnit.CoveredJourneyIds.Count > 1 || pricingUnit.FareComponents.Count > 1;

        private static AirPriceJourneyType JourneyTypeOf(PricingUnitKind kind) => kind switch
        {
            PricingUnitKind.OneWay => AirPriceJourneyType.OneWay,
            PricingUnitKind.ThroughOneWay => AirPriceJourneyType.OneWay,
            PricingUnitKind.RoundTripFare => AirPriceJourneyType.RoundTrip,
            PricingUnitKind.SectorSum => AirPriceJourneyType.OneWay,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        private sealed class Itinerary
        {
            private readonly Dictionary<long, OrderAirTransportService> _airServices;
            private readonly Dictionary<long, OrderSegment> _segments;
            private readonly Dictionary<long, OrderJourney> _journeys;
            private readonly Dictionary<long, OrderTraveller> _travellers;

            public Itinerary(Order order)
            {
                _airServices = order.Services.OfType<OrderAirTransportService>().ToDictionary(service => service.Id);
                _segments = order.Segments.ToDictionary(segment => segment.Id);
                _journeys = order.Journeys.ToDictionary(journey => journey.Id);
                _travellers = order.Travellers.ToDictionary(traveller => traveller.Id);
            }

            public OrderJourney Journey(long journeyId) => _journeys[journeyId];

            public OrderSegment SegmentOf(long airServiceId) => _segments[_airServices[airServiceId].SegmentId];

            public PricedService Priced(long airServiceId)
            {
                var service = _airServices[airServiceId];
                var segment = _segments[service.SegmentId];

                return service is { RbdId: { } rbdId } && segment is { FlightNumber: { } flightNumber, AircraftId: { } aircraftId }
                    ? new PricedService(segment, _travellers[service.TravellerId], rbdId, flightNumber, aircraftId)
                    : throw ExceptionFactory.AirServiceValidationFactsAreMissing(service.Id);
            }
        }

        private sealed record ScopedPricingUnit(OrderFarePricingUnit PricingUnit, IReadOnlyList<ScopedFareComponent> FareComponents);

        private sealed record ScopedFareComponent(OrderFareComponent Component, IReadOnlyList<PricedService> Services);

        private sealed record PricedService(OrderSegment Segment, OrderTraveller Traveller, long RbdId, string FlightNumber, int AircraftId);
    }
}
