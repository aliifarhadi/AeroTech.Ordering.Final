using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Enums;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public static class BackofficeOrderMapper
    {
        public static BackofficeOrderDto ToBackofficeOrder(OrderDetailDto order, ReferenceCodes codes)
            => new(
                order.Id,
                order.OrderReference,
                order.SourceOfferId,
                EnumValueDto.Of(order.Status),
                EnumValueDto.Of(order.Channel),
                order.CustomerId,
                ToSellingOffice(order, codes),
                order.CurrencyId,
                order.CustomerTotal,
                order.CommercialVersion,
                order.LastTicketingDate,
                order.CreatedAt,
                order.Travellers.Select(ToDto).ToList(),
                order.Journeys,
                order.Items.Select(ToDto).ToList(),
                order.PricingLines.Select(ToDto).ToList(),
                order.Contacts.Select(ToDto).ToList(),
                order.Remarks.Select(ToDto).ToList());

        private static SellingOfficeDto? ToSellingOffice(OrderDetailDto order, ReferenceCodes codes)
        {
            if (order.OfficeKind is not { } kind || order.OfficeId is not { } officeId)
                return null;

            var office = codes.Office(kind, officeId);

            return new SellingOfficeDto(EnumValueDto.Of(kind), officeId, office?.Code, office?.Name);
        }

        private static BackofficeOrderTravellerDto ToDto(OrderTravellerDto traveller)
            => new(
                traveller.Id,
                traveller.Index,
                EnumValueDto.Of(traveller.PassengerType),
                EnumValueDto.Of(traveller.AgeRange),
                EnumValueDto.Of(traveller.Status),
                traveller.GivenName,
                traveller.Surname,
                traveller.DateOfBirth,
                EnumValueDto.OfNullable(traveller.Gender),
                traveller.NationalityId,
                traveller.CountryOfResidenceId,
                traveller.InfantParentTravellerId,
                traveller.Documents.Select(ToDto).ToList());

        private static BackofficeOrderTravellerDocumentDto ToDto(OrderTravellerDocumentDto document)
            => new(
                document.Id,
                EnumValueDto.Of(document.Type),
                document.Number,
                document.ExpiryDate,
                document.IssuanceCountryId,
                document.Holder);

        private static BackofficeOrderItemDto ToDto(OrderItemDto item)
            => new(
                item.Id,
                EnumValueDto.Of(item.Kind),
                item.AcceptedTotal,
                EnumValueDto.Of(item.CommercialStatus),
                item.Services.Select(ToDto).ToList());

        private static BackofficeOrderServiceDto ToDto(OrderServiceDto service)
            => new(
                service.Id,
                service.TravellerId,
                service.SegmentId,
                EnumValueDto.Of(service.ServiceType),
                EnumValueDto.Of(service.CommercialStatus),
                service.BookingClass,
                service.FareBasis,
                service.FareFamily,
                service.FareType,
                ToDto(service.CheckedBaggage),
                ToDto(service.CabinBaggage),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable,
                service.SeatNumber);

        private static BackofficeBaggageAllowanceDto? ToDto(BaggageAllowanceDto? baggage)
            => baggage is null
                ? null
                : new BackofficeBaggageAllowanceDto(baggage.Pieces, baggage.Weight, EnumValueDto.Of(baggage.Unit));

        private static BackofficeOrderPricingLineDto ToDto(OrderPricingLineDto line)
            => new(
                line.Id,
                EnumValueDto.Of(line.Reason),
                EnumValueDto.Of(line.Scope),
                EnumValueDto.Of(line.Category),
                EnumValueDto.Of(line.SubCategory),
                EnumValueDto.Of(line.Direction),
                EnumValueDto.Of(line.Treatment),
                line.Code,
                line.Description,
                line.Reference,
                line.Amount,
                line.CurrencyId,
                line.EquivalentAmount,
                line.EquivalentCurrencyId,
                EnumValueDto.OfNullable(line.Refundability),
                line.Allocations);

        private static BackofficeOrderContactDto ToDto(OrderContactDto contact)
            => new(
                contact.Id,
                contact.Sequence,
                EnumValueDto.Of(contact.Role),
                contact.ContactName,
                contact.ContactPoints.Select(ToDto).ToList());

        private static BackofficeOrderContactPointDto ToDto(OrderContactPointDto point)
            => new(
                point.Id,
                EnumValueDto.Of(point.Type),
                point.Value,
                point.CountryCode,
                point.IsPrimary);

        private static BackofficeOrderRemarkDto ToDto(OrderRemarkDto remark)
            => new(
                remark.Id,
                EnumValueDto.Of(remark.Type),
                EnumValueDto.Of(remark.Visibility),
                EnumValueDto.Of(remark.Scope),
                remark.TravellerId,
                remark.SegmentId,
                remark.OrderItemId,
                remark.OrderServiceId,
                remark.Text,
                remark.CategoryCode,
                remark.IsPrintedOnItinerary,
                remark.IsPrintedOnInvoice,
                EnumValueDto.Of(remark.Status),
                remark.SupersedesRemarkId,
                remark.CreatedAt);
    }
}
