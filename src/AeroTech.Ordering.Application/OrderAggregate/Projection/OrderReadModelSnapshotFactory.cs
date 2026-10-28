using AeroTech.Messages.Ancillary.Enums;
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
                order.RecordLocator,
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
                Remarks = order.Remarks.Select(ToSnapshot).ToList(),
                Changes = order.Changes.Select(ToSnapshot).ToList()
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
                segment.FlightVersion,
                segment.FlightNumber,
                segment.MarketingAirlineId,
                segment.OperatingAirlineId,
                segment.OriginAirportId,
                segment.OriginAirportTerminalId,
                segment.DestinationAirportId,
                segment.DestinationAirportTerminalId,
                segment.SoldDeparture,
                segment.SoldArrival,
                segment.Duration,
                segment.AircraftId)
            {
                Legs = segment.Legs.Select(ToSnapshot).ToList()
            };

        private static OrderSegmentLegSnapshot ToSnapshot(OrderSegmentLeg leg)
            => new(
                leg.Id,
                leg.Sequence,
                leg.LegId,
                leg.OriginAirportId,
                leg.DestinationAirportId,
                leg.DepartureDateTime,
                leg.ArrivalDateTime);

        private static OrderItemSnapshot ToSnapshot(OrderItem item)
            => new(item.Id, item.Kind, item.AcceptedTotal, item.CommercialStatus, item.EndedByChangeId);

        private static OrderChangeSnapshot ToSnapshot(OrderChange change)
            => new(
                change.Id,
                change.ChangeType,
                change.CommercialVersion,
                change.ActorContext.ActorId,
                change.ActorContext.Channel,
                change.ActorContext.ContextType,
                change.ActorContext.PrincipalType,
                change.SourceReference,
                change.SourceSystem,
                change.ReasonCode,
                change.ReasonText,
                change.IsInvoluntary,
                change.WaiverCode,
                change.CommittedAt);

        private static OrderServiceSnapshot ToSnapshot(OrderService service) => service switch
        {
            OrderAirTransportService air => new OrderServiceSnapshot(
                air.Id,
                air.OrderItemId,
                air.TravellerId,
                air.SegmentId,
                air.ServiceType,
                air.CommercialStatus,
                air.FlightCapacityId,
                air.AirFareId,
                air.BookingClass,
                air.FareBasis,
                air.FareFamily,
                air.FareType,
                air.RbdId,
                air.CabinClassId,
                ToSnapshot(air.CheckedBaggageAllowance),
                ToSnapshot(air.CabinBaggageAllowance),
                air.IsRefundable,
                air.IsChangeable,
                air.IsUpgradable,
                SeatNumber: null,
                air.EndedByChangeId),

            OrderSeatService seat => new OrderServiceSnapshot(
                seat.Id,
                seat.OrderItemId,
                seat.TravellerId,
                seat.SegmentId,
                seat.ServiceType,
                seat.CommercialStatus,
                FlightCapacityId: null,
                AirFareId: null,
                BookingClass: null,
                FareBasis: null,
                FareFamily: null,
                FareType: null,
                RbdId: null,
                CabinClassId: null,
                CheckedBaggage: null,
                CabinBaggage: null,
                IsRefundable: false,
                IsChangeable: false,
                IsUpgradable: false,
                seat.SeatNumber,
                seat.EndedByChangeId)
            {
                Ancillary = seat.IsPricedSelection ? ToSnapshot(seat) : null
            },

            OrderAncillaryService ancillary => new OrderServiceSnapshot(
                ancillary.Id,
                ancillary.OrderItemId,
                ancillary.TravellerId,
                SegmentId: null,
                ancillary.ServiceType,
                ancillary.CommercialStatus,
                FlightCapacityId: null,
                AirFareId: null,
                BookingClass: null,
                FareBasis: null,
                FareFamily: null,
                FareType: null,
                RbdId: null,
                CabinClassId: null,
                CheckedBaggage: null,
                CabinBaggage: null,
                IsRefundable: false,
                IsChangeable: false,
                IsUpgradable: false,
                SeatNumber: null,
                ancillary.EndedByChangeId)
            {
                Ancillary = ToSnapshot(ancillary)
            },

            _ => throw new NotSupportedException($"{service.GetType().Name} has no read model projection.")
        };

        private static OrderAncillarySnapshot ToSnapshot(OrderAncillaryService ancillary)
        {
            var snapshot = new OrderAncillarySnapshot(
                ancillary.VariantCode,
                ancillary.Profile,
                ancillary.Quantity,
                ancillary.SelectionScope,
                ancillary.CoverageScope,
                ancillary.CoveredAirServiceIds.ToList(),
                PointOfSaleId: null,
                ancillary.Source,
                ancillary.Definition,
                ancillary.Provision,
                ancillary.Pricing);

            return ancillary switch
            {
                OrderBaggageService baggage => snapshot with
                {
                    Baggage = new AncillaryBaggageSnapshot(
                        baggage.ChargeKind,
                        baggage.AllowanceConcept,
                        baggage.PackageWeightKg,
                        baggage.WeightFromExclusiveKg,
                        baggage.WeightToInclusiveKg,
                        baggage.EquipmentKind,
                        baggage.BagRef,
                        baggage.MeasuredWeightKg,
                        baggage.Dimensions,
                        baggage.WeightKg)
                },
                OrderMealService meal => snapshot with
                {
                    Meal = new AncillaryMealSnapshot(meal.MealKind, meal.MealCode, meal.MenuItemRef, meal.DietaryCode, meal.ExclusiveMealFamilyCode)
                },
                OrderPetService pet => snapshot with
                {
                    Pet = new AncillaryPetSnapshot(
                        pet.TransportMode,
                        pet.AnimalType,
                        pet.OtherAnimalCode,
                        pet.AnimalAgeWeeks,
                        pet.CombinedWeightKg,
                        pet.CarrierDimensions,
                        pet.DocumentAcknowledgements.ToList(),
                        pet.SizeBracket)
                },
                OrderAssistedTravelService assistance => snapshot with
                {
                    Assistance = new AncillaryAssistanceSnapshot(assistance.AssistanceKind, assistance.AssistanceSsrCode, assistance.InfantTravellerId, assistance.GuardianTravellerId)
                },
                OrderAirportService airport => snapshot with
                {
                    AirportService = new AncillaryAirportServiceSnapshot(
                        airport.Kind,
                        airport.AirportId,
                        airport.TerminalRef,
                        airport.FacilityId,
                        airport.Direction,
                        airport.TimeWithOffset,
                        airport.GuestCount)
                },
                OrderPriorityService priority => snapshot with
                {
                    Priority = new AncillaryPrioritySnapshot(priority.Kind, priority.PriorityZoneCode, priority.PriorityGroupCode, priority.FareBenefitRef, priority.OptIn)
                },
                OrderConnectivityService connectivity => snapshot with
                {
                    Connectivity = new AncillaryConnectivitySnapshot(
                        connectivity.PlanKind,
                        connectivity.DurationMinutes,
                        connectivity.IncludedDataMb,
                        connectivity.MaxDevices,
                        connectivity.DeliveryStage,
                        connectivity.DeviceCount)
                },
                OrderUpgradeService upgrade => snapshot with
                {
                    Upgrade = new AncillaryUpgradeSnapshot(upgrade.FromCabinId, upgrade.ToCabinId, upgrade.UpgradeKind, upgrade.RequiresTicketExchange)
                },
                OrderExtraSeatService => snapshot,
                _ => throw new NotSupportedException($"{ancillary.GetType().Name} has no read model projection.")
            };
        }

        private static OrderAncillarySnapshot ToSnapshot(OrderSeatService seat)
            => new(
                seat.VariantCode!,
                AncillaryProfile.Seat,
                Quantity: 1,
                SelectionScope: null,
                CoverageScope: null,
                [seat.AssociatedAirServiceId],
                seat.PointOfSaleId,
                seat.Source!,
                seat.Definition!,
                seat.Provision!,
                seat.Pricing!);

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
