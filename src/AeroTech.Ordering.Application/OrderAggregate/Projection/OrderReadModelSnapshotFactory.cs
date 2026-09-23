using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application.OrderAggregate.Projection
{
    internal static class OrderReadModelSnapshotFactory
    {
        public static OrderReadModelSnapshot ToReadModelSnapshot(this Order order, DateTimeOffset occurredAt)
            => new(
                order.Id,
                order.OrderReference,
                order.SourceOfferId,
                order.Status,
                order.SalesContext.Channel,
                order.CustomerId,
                order.SalesContext.ActorId,
                order.SalesContext.TravelAgencyId,
                order.SalesContext.OfficeKind,
                order.SalesContext.OfficeId,
                order.CurrencyId,
                order.CustomerTotal,
                order.CommercialVersion,
                order.LastTicketingDate,
                order.CreatedAt,
                occurredAt)
            {
                Travellers = order.Travellers.Select(ToSnapshot).ToList(),
                Journeys = order.Journeys.Select(ToSnapshot).ToList(),
                Segments = order.Segments.Select(ToSnapshot).ToList(),
                Items = order.Items.Select(ToSnapshot).ToList(),
                Services = order.Services.Select(ToSnapshot).ToList(),
                PricingLines = order.PricingLines.Select(ToSnapshot).ToList(),
                Contacts = order.Contacts.Select(ToSnapshot).ToList(),
                Remarks = order.Remarks.Select(ToSnapshot).ToList()
            };

        private static OrderTravellerSnapshot ToSnapshot(OrderTraveller traveller)
        {
            var profile = traveller.ProfileRevisions.Single(revision => revision.Id == traveller.CurrentProfileRevisionId);

            return new OrderTravellerSnapshot(
                traveller.Id,
                traveller.Index,
                traveller.SourceTravellerRef,
                traveller.PassengerType,
                traveller.AgeRange,
                traveller.InfantParentTravellerId,
                traveller.Status,
                profile.GivenName,
                profile.Surname,
                profile.DateOfBirth,
                profile.Gender,
                profile.NationalityId,
                profile.CountryOfResidenceId)
            {
                Documents = traveller.Documents.Select(ToSnapshot).ToList()
            };
        }

        private static OrderTravellerDocumentSnapshot ToSnapshot(OrderTravellerDocument document)
            => new(
                document.Id,
                document.Type,
                document.Number,
                document.ExpiryDate,
                document.IssuanceCountryId,
                document.Holder);

        private static OrderJourneySnapshot ToSnapshot(OrderJourney journey)
            => new(
                journey.Id,
                journey.Sequence,
                journey.BoundId,
                journey.OriginAirportId,
                journey.DestinationAirportId);

        private static OrderSegmentSnapshot ToSnapshot(OrderSegment segment)
            => new(
                segment.Id,
                segment.OrderJourneyId,
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

        private static OrderItemSnapshot ToSnapshot(OrderItem item)
            => new(item.Id, item.Kind, item.AcceptedTotal, item.CommercialStatus);

        private static OrderServiceSnapshot ToSnapshot(OrderService service) => service switch
        {
            OrderAirTransportService air => new OrderServiceSnapshot(
                air.Id,
                air.OrderItemId,
                air.TravellerId,
                air.SegmentId,
                air.ServiceType,
                air.CommercialStatus,
                air.BookingClass,
                air.FareBasis,
                air.FareFamily,
                air.FareType,
                ToSnapshot(air.CheckedBaggageAllowance),
                ToSnapshot(air.CabinBaggageAllowance),
                air.IsRefundable,
                air.IsChangeable,
                air.IsUpgradable,
                SeatNumber: null),

            OrderSeatService seat => new OrderServiceSnapshot(
                seat.Id,
                seat.OrderItemId,
                seat.TravellerId,
                seat.SegmentId,
                seat.ServiceType,
                seat.CommercialStatus,
                BookingClass: null,
                FareBasis: null,
                FareFamily: null,
                FareType: null,
                CheckedBaggage: null,
                CabinBaggage: null,
                IsRefundable: false,
                IsChangeable: false,
                IsUpgradable: false,
                seat.SeatNumber),

            _ => throw new NotSupportedException($"{service.GetType().Name} has no read model projection.")
        };

        private static BaggageAllowanceSnapshot? ToSnapshot(BaggageAllowance? allowance)
            => allowance is null ? null : new BaggageAllowanceSnapshot(allowance.Pieces, allowance.Weight, allowance.Unit);

        private static OrderPricingLineSnapshot ToSnapshot(PricingLine line)
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
                line.Refundability)
            {
                Allocations = line.Allocations.Select(ToSnapshot).ToList()
            };

        private static OrderPricingAllocationSnapshot ToSnapshot(PricingAllocation allocation)
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

        private static OrderContactSnapshot ToSnapshot(OrderContact contact)
            => new(contact.Id, contact.Sequence, contact.Role, contact.ContactName)
            {
                ContactPoints = contact.ContactPoints.Select(ToSnapshot).ToList()
            };

        private static OrderContactPointSnapshot ToSnapshot(OrderContactPoint point)
            => new(point.Id, point.Type, point.Value, point.CountryCode, point.IsPrimary);

        private static OrderRemarkSnapshot ToSnapshot(OrderRemark remark)
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
                remark.CreatedBy,
                remark.CreatedAt);
    }
}
