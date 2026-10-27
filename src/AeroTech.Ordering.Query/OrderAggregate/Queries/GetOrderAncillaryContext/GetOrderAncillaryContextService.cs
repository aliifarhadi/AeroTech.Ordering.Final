using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query.ElectronicTicketAggregate.Models;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query.OrderAggregate.Models;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext
{
    public sealed class GetOrderAncillaryContextService : IGetOrderAncillaryContextService
    {
        private const bool AcceptsNewServices = false;

        private readonly OrderQueryDbContext _dbContext;

        public GetOrderAncillaryContextService(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public async Task<OrderAncillaryContextDto?> ExecuteAsync(long orderId, CancellationToken cancellationToken = default)
        {
            var order = await _dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(row => row.Id == orderId, cancellationToken);

            if (order is null)
                return null;

            var travellers = await _dbContext.OrderTravellers.AsNoTracking()
                .Where(traveller => traveller.OrderId == orderId && traveller.Status == OrderTravellerStatus.Active)
                .OrderBy(traveller => traveller.Index)
                .ToListAsync(cancellationToken);
            var airServices = await _dbContext.OrderServices.AsNoTracking()
                .Where(service => service.OrderId == orderId
                                  && service.ServiceType == OrderServiceType.AirTransportation
                                  && service.CommercialStatus == OrderServiceCommercialState.Active)
                .OrderBy(service => service.TravellerId)
                .ThenBy(service => service.SegmentId)
                .ToListAsync(cancellationToken);
            var coveredSegmentIds = airServices.Select(service => service.SegmentId).ToHashSet();
            var segments = (await OwnedBy(_dbContext.OrderSegments, orderId, cancellationToken))
                .Where(segment => coveredSegmentIds.Contains(segment.Id))
                .ToList();
            var legsBySegment = (await OwnedBy(_dbContext.OrderSegmentLegs, orderId, cancellationToken))
                .ToLookup(leg => leg.OrderSegmentId);
            var segmentsByJourney = segments.ToLookup(segment => segment.OrderJourneyId);
            var journeys = (await OwnedBy(_dbContext.OrderJourneys, orderId, cancellationToken))
                .Where(journey => segmentsByJourney.Contains(journey.Id))
                .OrderBy(journey => journey.Sequence)
                .ToList();
            var couponsByService = (await OwnedBy(_dbContext.TicketCoupons, orderId, cancellationToken))
                .ToLookup(coupon => coupon.CurrentOrderServiceId);
            var ticketedAt = await _dbContext.ElectronicTickets.AsNoTracking()
                .Where(ticket => ticket.OrderId == orderId && ticket.StatusSummary != ElectronicTicketStatus.Voided)
                .Select(ticket => (DateTimeOffset?)ticket.IssuedAt)
                .MinAsync(cancellationToken);

            return new OrderAncillaryContextDto(
                order.Id,
                order.CommercialVersion,
                AcceptsNewServices,
                ticketedAt,
                order.SourceOfferId,
                order.CurrencyId,
                order.CustomerId,
                new OrderAncillarySalesDto(order.Channel, order.TravelAgencyId),
                travellers.Select(ToDto).ToList(),
                journeys.Select(journey => ToDto(journey, segmentsByJourney[journey.Id], legsBySegment)).ToList(),
                airServices.Select(service => ToDto(service, couponsByService[service.Id])).ToList(),
                []);
        }

        private static Task<List<TReadModel>> OwnedBy<TReadModel>(
            DbSet<TReadModel> set,
            long orderId,
            CancellationToken cancellationToken)
            where TReadModel : class, IOrderOwnedReadModel
            => set.AsNoTracking().Where(row => row.OrderId == orderId).ToListAsync(cancellationToken);

        private static OrderAncillaryTravellerDto ToDto(OrderTravellerReadModel traveller)
            => new(
                traveller.Id,
                traveller.PassengerType,
                traveller.DateOfBirth,
                traveller.InfantParentTravellerId);

        private static OrderAncillaryJourneyDto ToDto(
            OrderJourneyReadModel journey,
            IEnumerable<OrderSegmentReadModel> segments,
            ILookup<long, OrderSegmentLegReadModel> legsBySegment)
            => new(
                journey.BoundId,
                journey.Sequence,
                journey.OriginAirportId,
                journey.DestinationAirportId,
                segments.OrderBy(segment => segment.Sequence).Select(segment => ToDto(segment, legsBySegment[segment.Id])).ToList());

        private static OrderAncillarySegmentDto ToDto(OrderSegmentReadModel segment, IEnumerable<OrderSegmentLegReadModel> legs)
            => new(
                segment.Id,
                segment.Sequence,
                segment.FlightId,
                segment.FlightVersion,
                segment.FlightNumber,
                segment.OriginAirportId,
                segment.OriginAirportTerminalId,
                segment.DestinationAirportId,
                segment.DestinationAirportTerminalId,
                segment.OperatingAirlineId,
                segment.MarketingAirlineId,
                segment.SoldDeparture,
                segment.SoldArrival,
                segment.AircraftId,
                legs.OrderBy(leg => leg.Sequence).Select(ToDto).ToList());

        private static OrderAncillaryLegDto ToDto(OrderSegmentLegReadModel leg)
            => new(
                leg.LegId,
                leg.Sequence,
                leg.OriginAirportId,
                leg.DestinationAirportId,
                leg.DepartureDateTime,
                leg.ArrivalDateTime);

        private static OrderAncillaryAirServiceDto ToDto(OrderServiceReadModel service, IEnumerable<TicketCouponReadModel> coupons)
            => new(
                service.Id,
                service.TravellerId,
                service.SegmentId,
                service.FlightCapacityId ?? throw new InvalidOperationException($"Air service {service.Id} has no projected flight capacity."),
                service.AirFareId,
                service.FareBasis,
                service.FareFamily,
                service.FareType,
                service.BookingClass,
                service.RbdId,
                service.CabinClassId,
                BaggageAllowanceDto.Of(service.CheckedBaggagePieces, service.CheckedBaggageWeight, service.CheckedBaggageUnit),
                BaggageAllowanceDto.Of(service.CabinBaggagePieces, service.CabinBaggageWeight, service.CabinBaggageUnit),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable,
                CouponOpenForUse(coupons.ToList()));

        private static bool? CouponOpenForUse(IReadOnlyCollection<TicketCouponReadModel> coupons)
            => coupons.Count == 0
                ? null
                : coupons.Any(coupon => coupon.FinancialStatus == TicketCouponFinancialStatus.Open);
    }
}
