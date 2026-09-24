using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query.OrderAggregate.Models;
using AeroTech.Ordering.Query._Shared.Authorization;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public sealed class GetOrderByIdService : IGetOrderByIdService
    {
        private readonly OrderQueryDbContext _dbContext;

        public GetOrderByIdService(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public async Task<OrderDetailDto?> ExecuteAsync(
            long orderId,
            OrderQueryScope scope,
            CancellationToken cancellationToken = default)
        {
            var orders = _dbContext.Orders.AsNoTracking().Where(row => row.Id == orderId);

            if (scope.CustomerId is { } customerId)
                orders = orders.Where(row => row.CustomerId == customerId);

            var order = await orders.FirstOrDefaultAsync(cancellationToken);

            if (order is null)
                return null;

            var travellers = await OwnedBy(_dbContext.OrderTravellers, orderId, cancellationToken);
            var documents = await OwnedBy(_dbContext.OrderTravellerDocuments, orderId, cancellationToken);
            var journeys = await OwnedBy(_dbContext.OrderJourneys, orderId, cancellationToken);
            var segments = await OwnedBy(_dbContext.OrderSegments, orderId, cancellationToken);
            var items = await OwnedBy(_dbContext.OrderItems, orderId, cancellationToken);
            var services = await OwnedBy(_dbContext.OrderServices, orderId, cancellationToken);
            var pricingLines = await OwnedBy(_dbContext.OrderPricingLines, orderId, cancellationToken);
            var allocations = await OwnedBy(_dbContext.OrderPricingAllocations, orderId, cancellationToken);
            var contacts = await OwnedBy(_dbContext.OrderContacts, orderId, cancellationToken);
            var contactPoints = await OwnedBy(_dbContext.OrderContactPoints, orderId, cancellationToken);
            var remarks = await OwnedBy(_dbContext.OrderRemarks, orderId, cancellationToken);

            var documentsByTraveller = documents.ToLookup(document => document.TravellerId);
            var segmentsByJourney = segments.ToLookup(segment => segment.OrderJourneyId);
            var servicesByItem = services.ToLookup(service => service.OrderItemId);
            var allocationsByLine = allocations.ToLookup(allocation => allocation.PricingLineId);
            var pointsByContact = contactPoints.ToLookup(point => point.OrderContactId);

            return new OrderDetailDto(
                order.Id,
                order.OrderReference,
                order.RecordLocator,
                order.SourceOfferId,
                order.Status,
                order.Channel,
                order.CustomerId,
                order.OfficeKind,
                order.OfficeId,
                order.CurrencyId,
                order.CustomerTotal,
                order.CommercialVersion,
                order.LastTicketingDate,
                order.CreatedAt,
                travellers.OrderBy(traveller => traveller.Index).Select(traveller => ToDto(traveller, documentsByTraveller[traveller.Id])).ToList(),
                journeys.OrderBy(journey => journey.Sequence).Select(journey => ToDto(journey, segmentsByJourney[journey.Id])).ToList(),
                items.Select(item => ToDto(item, servicesByItem[item.Id])).ToList(),
                pricingLines.Select(line => ToDto(line, allocationsByLine[line.Id])).ToList(),
                contacts.OrderBy(contact => contact.Sequence).Select(contact => ToDto(contact, pointsByContact[contact.Id])).ToList(),
                remarks.OrderBy(remark => remark.CreatedAt).Select(ToDto).ToList());
        }

        private static Task<List<TReadModel>> OwnedBy<TReadModel>(
            DbSet<TReadModel> set,
            long orderId,
            CancellationToken cancellationToken)
            where TReadModel : class, IOrderOwnedReadModel
            => set.AsNoTracking().Where(row => row.OrderId == orderId).ToListAsync(cancellationToken);

        private static OrderTravellerDto ToDto(
            OrderTravellerReadModel traveller,
            IEnumerable<OrderTravellerDocumentReadModel> documents)
            => new(
                traveller.Id,
                traveller.Index,
                traveller.PassengerType,
                traveller.AgeRange,
                traveller.Status,
                traveller.GivenName,
                traveller.Surname,
                traveller.DateOfBirth,
                traveller.Gender,
                traveller.NationalityId,
                traveller.CountryOfResidenceId,
                traveller.InfantParentTravellerId,
                documents.Select(ToDto).ToList());

        private static OrderTravellerDocumentDto ToDto(OrderTravellerDocumentReadModel document)
            => new(
                document.Id,
                document.Type,
                document.Number,
                document.ExpiryDate,
                document.IssuanceCountryId,
                document.Holder);

        private static OrderJourneyDto ToDto(OrderJourneyReadModel journey, IEnumerable<OrderSegmentReadModel> segments)
            => new(
                journey.Id,
                journey.Sequence,
                journey.BoundId,
                journey.OriginAirportId,
                journey.DestinationAirportId,
                segments.OrderBy(segment => segment.Sequence).Select(ToDto).ToList());

        private static OrderSegmentDto ToDto(OrderSegmentReadModel segment)
            => new(
                segment.Id,
                segment.Sequence,
                segment.FlightId,
                segment.FlightNumber,
                segment.MarketingAirlineId,
                segment.OperatingAirlineId,
                segment.OriginAirportId,
                segment.DestinationAirportId,
                segment.SoldDeparture,
                segment.SoldArrival,
                segment.Duration);

        private static OrderItemDto ToDto(OrderItemReadModel item, IEnumerable<OrderServiceReadModel> services)
            => new(
                item.Id,
                item.Kind,
                item.AcceptedTotal,
                item.CommercialStatus,
                services.Select(ToDto).ToList());

        private static OrderServiceDto ToDto(OrderServiceReadModel service)
            => new(
                service.Id,
                service.TravellerId,
                service.SegmentId,
                service.ServiceType,
                service.CommercialStatus,
                service.BookingClass,
                service.FareBasis,
                service.FareFamily,
                service.FareType,
                ToBaggageDto(service.CheckedBaggagePieces, service.CheckedBaggageWeight, service.CheckedBaggageUnit),
                ToBaggageDto(service.CabinBaggagePieces, service.CabinBaggageWeight, service.CabinBaggageUnit),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable,
                service.SeatNumber);

        private static BaggageAllowanceDto? ToBaggageDto(int? pieces, decimal? weight, WeightUnit? unit)
            => pieces is null || weight is null || unit is null
                ? null
                : new BaggageAllowanceDto(pieces.Value, weight.Value, unit.Value);

        private static OrderPricingLineDto ToDto(OrderPricingLineReadModel line, IEnumerable<OrderPricingAllocationReadModel> allocations)
            => new(
                line.Id,
                line.Reason,
                line.Scope,
                line.Category,
                line.SubCategory,
                line.Direction,
                line.Treatment,
                line.Code,
                line.Description,
                line.Reference,
                line.Amount,
                line.CurrencyId,
                line.EquivalentAmount,
                line.EquivalentCurrencyId,
                line.Refundability,
                allocations.Select(ToDto).ToList());

        private static OrderPricingAllocationDto ToDto(OrderPricingAllocationReadModel allocation)
            => new(
                allocation.Id,
                allocation.OrderItemId,
                allocation.OrderServiceId,
                allocation.OrderJourneyId,
                allocation.OrderSegmentId,
                allocation.TravellerId,
                allocation.Amount,
                allocation.CurrencyId,
                allocation.EquivalentAmount,
                allocation.EquivalentCurrencyId);

        private static OrderContactDto ToDto(OrderContactReadModel contact, IEnumerable<OrderContactPointReadModel> contactPoints)
            => new(
                contact.Id,
                contact.Sequence,
                contact.Role,
                contact.ContactName,
                contactPoints.Select(ToDto).ToList());

        private static OrderContactPointDto ToDto(OrderContactPointReadModel point)
            => new(point.Id, point.Type, point.Value, point.CountryCode, point.IsPrimary);

        private static OrderRemarkDto ToDto(OrderRemarkReadModel remark)
            => new(
                remark.Id,
                remark.Type,
                remark.Visibility,
                remark.Scope,
                remark.TravellerId,
                remark.SegmentId,
                remark.OrderItemId,
                remark.OrderServiceId,
                remark.Text,
                remark.CategoryCode,
                remark.IsPrintedOnItinerary,
                remark.IsPrintedOnInvoice,
                remark.Status,
                remark.SupersedesRemarkId,
                remark.CreatedAt);
    }
}
