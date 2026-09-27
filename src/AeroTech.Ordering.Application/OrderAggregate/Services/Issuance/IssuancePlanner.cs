using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Issuance
{
    public sealed class IssuancePlanner : IIssuancePlanner
    {
        private readonly IReservationProviderResolver _providers;

        public IssuancePlanner(IReservationProviderResolver providers) => _providers = providers;

        public IReadOnlyList<OrderAirTransportService> OutstandingServices(Order order, IReadOnlyCollection<ElectronicTicket> tickets)
        {
            var documentedServiceIds = TicketCoverage.DocumentedServiceIds(tickets);

            return order.TicketableAirServices().Where(service => !documentedServiceIds.Contains(service.Id)).ToList();
        }

        public IssuanceScope ScopeOf(
            IReadOnlyList<OrderAirTransportService> services,
            IReadOnlyCollection<FulfillmentReservation> reservations)
        {
            var coverageByService = LatestCoverageByService(reservations);
            var coveringReservations = new List<FulfillmentReservation>();

            foreach (var service in services.Where(_providers.RequiresReservation))
            {
                if (!coverageByService.TryGetValue(service.Id, out var coverage))
                    throw ExceptionFactory.AirServiceIsNotReserved(service.Id);

                if (coverage.Reservation.Status != FulfillmentReservationStatus.Confirmed)
                    throw ExceptionFactory.AirServiceCapacityIsNotConfirmed(service.Id, coverage.Reservation.Status);

                if (coverage.UnitStatus != ReservationMemberStatus.Confirmed)
                    throw ExceptionFactory.AirServiceCapacityIsNotConfirmed(service.Id, coverage.UnitStatus);

                if (!coveringReservations.Contains(coverage.Reservation))
                    coveringReservations.Add(coverage.Reservation);
            }

            return new IssuanceScope(services, coveringReservations);
        }

        public IReadOnlyList<TicketPlan> PlanTickets(Order order, IssuanceScope scope)
        {
            var travellers = order.Travellers.ToDictionary(traveller => traveller.Id);
            var segments = order.Segments.ToDictionary(segment => segment.Id);
            var journeys = order.Journeys.ToDictionary(journey => journey.Id);
            var fareComponents = order.FarePricingUnits.SelectMany(unit => unit.FareComponents).ToList();
            var customerAllocations = order.PricingLines
                .Where(line => line.Treatment == PricingLineTreatment.CustomerPrice)
                .SelectMany(line => line.Allocations.Select(allocation => new AttributedAllocation(line, allocation)))
                .Where(attributed => attributed.Allocation.OrderServiceId is not null)
                .ToLookup(attributed => attributed.Allocation.OrderServiceId!.Value);

            return scope.Services
                .GroupBy(service => service.TravellerId)
                .OrderBy(group => travellers[group.Key].Index)
                .Select(group => new TicketPlan(
                    group.Key,
                    travellers[group.Key].CurrentProfileRevisionId,
                    $"ETKT:{group.Key}:1",
                    group
                        .OrderBy(service => journeys[segments[service.SegmentId].OrderJourneyId].Sequence)
                        .ThenBy(service => segments[service.SegmentId].Sequence)
                        .ThenBy(service => service.Id)
                        .Select(service => CouponOf(order, service, segments[service.SegmentId], fareComponents, customerAllocations[service.Id]))
                        .ToList()))
                .ToList();
        }

        private static IReadOnlyDictionary<long, Coverage> LatestCoverageByService(IEnumerable<FulfillmentReservation> reservations)
            => reservations
                .OrderBy(reservation => reservation.CreatedAt)
                .ThenBy(reservation => reservation.Id)
                .SelectMany(reservation => reservation.Units.SelectMany(unit => unit.OrderServiceIds
                    .Select(serviceId => (ServiceId: serviceId, Coverage: new Coverage(reservation, unit.Status)))))
                .GroupBy(coverage => coverage.ServiceId)
                .ToDictionary(group => group.Key, group => group.Last().Coverage);

        private static IssueTicketCouponArgs CouponOf(
            Order order,
            OrderAirTransportService service,
            OrderSegment segment,
            IReadOnlyList<OrderFareComponent> fareComponents,
            IEnumerable<AttributedAllocation> allocations)
        {
            var fareComponent = FareComponentOf(service, fareComponents);
            var priceLinks = allocations.Select(attributed => PriceLinkOf(order, service, attributed)).ToList();

            if (priceLinks.Count == 0)
                throw ExceptionFactory.AirServiceTicketPricingIsMissing(service.Id);

            var issuanceValue = priceLinks.Sum(link => link.AttributedValue);

            if (issuanceValue < 0)
                throw ExceptionFactory.TicketCouponIssuanceValueIsNegative(service.Id, issuanceValue);

            return new IssueTicketCouponArgs(
                service.Id,
                service.TravellerId,
                service.SegmentId,
                fareComponent.Id,
                new IssuedSegmentSnapshot(
                    segment.MarketingAirlineId,
                    segment.OperatingAirlineId,
                    segment.FlightNumber,
                    segment.OriginAirportId,
                    segment.DestinationAirportId,
                    segment.SoldDeparture,
                    segment.SoldArrival,
                    service.BookingClass,
                    service.RbdId,
                    service.CabinClassId,
                    null),
                service.FareBasis,
                service.BookingClass,
                service.RbdId,
                service.CabinClassId,
                SnapshotOf(service.CheckedBaggageAllowance),
                priceLinks);
        }

        private static BaggageAllowance? SnapshotOf(BaggageAllowance? allowance)
            => allowance is null ? null : new BaggageAllowance(allowance.Pieces, allowance.Weight, allowance.Unit);

        private static OrderFareComponent FareComponentOf(OrderAirTransportService service, IReadOnlyList<OrderFareComponent> fareComponents)
        {
            var covering = fareComponents.Where(component => component.CoveredOrderServiceIds.Contains(service.Id)).ToList();

            return covering.Count switch
            {
                0 => throw ExceptionFactory.AirServiceFareComponentIsMissing(service.Id),
                1 => covering[0],
                _ => throw ExceptionFactory.AirServiceFareComponentIsAmbiguous(service.Id, covering.Count)
            };
        }

        private static IssueTicketPriceLinkArgs PriceLinkOf(Order order, OrderAirTransportService service, AttributedAllocation attributed)
        {
            var allocation = attributed.Allocation;

            if (allocation.TravellerId is { } travellerId && travellerId != service.TravellerId
                || allocation.OrderSegmentId is { } segmentId && segmentId != service.SegmentId)
                throw ExceptionFactory.AirServiceTicketPricingIsAmbiguous(service.Id, allocation.Id);

            if (allocation.EquivalentCurrencyId != order.CurrencyId)
                throw ExceptionFactory.TicketPricingCurrencyMismatch(service.Id, allocation.EquivalentCurrencyId, order.CurrencyId);

            var attributedValue = attributed.Line.Direction == OrderPricingLineDirection.Credit
                ? allocation.EquivalentAmount
                : -allocation.EquivalentAmount;

            return new IssueTicketPriceLinkArgs(attributed.Line.Id, allocation.Id, attributedValue, allocation.EquivalentCurrencyId);
        }

        private sealed record Coverage(FulfillmentReservation Reservation, ReservationMemberStatus UnitStatus);

        private sealed record AttributedAllocation(PricingLine Line, PricingAllocation Allocation);
    }
}
