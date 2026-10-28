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
                order.RecordLocator,
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
                order.Remarks.Select(ToDto).ToList(),
                order.Tickets.Select(ToDto).ToList(),
                order.Changes.Select(ToDto).ToList());

        private static SellingOfficeDto? ToSellingOffice(OrderDetailDto order, ReferenceCodes codes)
        {
            if (order.OfficeKind is not { } kind || order.OfficeId is not { } officeId)
                return null;

            var office = codes.Office(kind, officeId);

            return new SellingOfficeDto(EnumValueDto.Of(kind), officeId, office?.Code, office?.Name);
        }

        private static BackofficeElectronicTicketDto ToDto(OrderElectronicTicketDto ticket)
            => new(
                ticket.Id,
                ticket.TravellerId,
                ticket.DocumentNumber,
                EnumValueDto.Of(ticket.Authority),
                ticket.IssuedAt,
                ticket.IssuedTotal,
                ticket.CurrencyId,
                EnumValueDto.Of(ticket.StatusSummary),
                ticket.DocumentVersion,
                ticket.VoidDeadline,
                ticket.VoidRecord,
                ticket.Coupons.Select(ToDto).ToList());

        private static BackofficeOrderChangeDto ToDto(OrderChangeDto change)
            => new(
                change.Id,
                EnumValueDto.Of(change.ChangeType),
                change.CommercialVersion,
                change.ActorId,
                EnumValueDto.Of(change.Channel),
                EnumValueDto.Of(change.ContextType),
                EnumValueDto.Of(change.PrincipalType),
                change.SourceReference,
                change.SourceSystem,
                change.ReasonCode,
                change.ReasonText,
                change.IsInvoluntary,
                change.WaiverCode,
                change.CommittedAt);

        private static BackofficeTicketCouponDto ToDto(OrderTicketCouponDto coupon)
            => new(
                coupon.Id,
                coupon.CouponNumber,
                coupon.OriginalOrderServiceId,
                coupon.CurrentOrderServiceId,
                coupon.OrderSegmentId,
                coupon.FareBasisSnapshot,
                coupon.BookingClassSnapshot,
                coupon.RbdIdSnapshot,
                coupon.CabinClassIdSnapshot,
                coupon.IssuanceValue,
                coupon.CurrencyId,
                EnumValueDto.Of(coupon.FinancialStatus),
                EnumValueDto.Of(coupon.ControlStatus));

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
                item.EndedByChangeId,
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
                EnumValueDto.OfNullable(service.FareType),
                ToDto(service.CheckedBaggage),
                ToDto(service.CabinBaggage),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable,
                service.SeatNumber,
                service.EndedByChangeId,
                service.Ancillary is null ? null : ToDto(service.Ancillary));

        private static BackofficeOrderAncillaryDto ToDto(OrderAncillaryDto ancillary)
            => new(
                ancillary.VariantCode,
                EnumValueDto.Of(ancillary.Profile),
                ancillary.Quantity,
                EnumValueDto.OfNullable(ancillary.SelectionScope),
                EnumValueDto.OfNullable(ancillary.CoverageScope),
                ancillary.CoveredAirServiceIds,
                ancillary.ServiceDefinitionRef,
                ancillary.ServiceTypeCode,
                ancillary.ServiceSubCode,
                ancillary.Rfic,
                ancillary.Rfisc,
                ancillary.SsrCode,
                ancillary.Baggage is { } baggage
                    ? new BackofficeAncillaryBaggageDto(
                        EnumValueDto.Of(baggage.ChargeKind),
                        EnumValueDto.OfNullable(baggage.AllowanceConcept),
                        baggage.PackageWeightKg,
                        baggage.WeightFromExclusiveKg,
                        baggage.WeightToInclusiveKg,
                        baggage.EquipmentKind,
                        baggage.BagRef,
                        baggage.MeasuredWeightKg,
                        baggage.Dimensions,
                        baggage.WeightKg)
                    : null,
                ancillary.Meal is { } meal
                    ? new BackofficeAncillaryMealDto(EnumValueDto.Of(meal.MealKind), meal.MealCode, meal.MenuItemRef, meal.DietaryCode, meal.ExclusiveMealFamilyCode)
                    : null,
                ancillary.Pet is { } pet
                    ? new BackofficeAncillaryPetDto(
                        EnumValueDto.Of(pet.TransportMode),
                        EnumValueDto.Of(pet.AnimalType),
                        pet.OtherAnimalCode,
                        pet.AnimalAgeWeeks,
                        pet.CombinedWeightKg,
                        pet.CarrierDimensions,
                        pet.DocumentAcknowledgements,
                        pet.SizeBracket)
                    : null,
                ancillary.Assistance is { } assistance
                    ? new BackofficeAncillaryAssistanceDto(EnumValueDto.Of(assistance.AssistanceKind), assistance.AssistanceSsrCode, assistance.InfantTravellerId, assistance.GuardianTravellerId)
                    : null,
                ancillary.AirportService is { } airport
                    ? new BackofficeAncillaryAirportServiceDto(
                        EnumValueDto.Of(airport.Kind),
                        airport.AirportId,
                        airport.TerminalRef,
                        airport.FacilityId,
                        EnumValueDto.Of(airport.Direction),
                        airport.TimeWithOffset,
                        airport.GuestCount)
                    : null,
                ancillary.Priority is { } priority
                    ? new BackofficeAncillaryPriorityDto(EnumValueDto.Of(priority.Kind), priority.PriorityZoneCode, priority.PriorityGroupCode, priority.FareBenefitRef, priority.OptIn)
                    : null,
                ancillary.Connectivity is { } connectivity
                    ? new BackofficeAncillaryConnectivityDto(
                        EnumValueDto.Of(connectivity.PlanKind),
                        connectivity.DurationMinutes,
                        connectivity.IncludedDataMb,
                        connectivity.MaxDevices,
                        EnumValueDto.Of(connectivity.DeliveryStage),
                        connectivity.DeviceCount)
                    : null,
                ancillary.Upgrade is { } upgrade
                    ? new BackofficeAncillaryUpgradeDto(upgrade.FromCabinId, upgrade.ToCabinId, EnumValueDto.Of(upgrade.UpgradeKind), upgrade.RequiresTicketExchange)
                    : null);

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
