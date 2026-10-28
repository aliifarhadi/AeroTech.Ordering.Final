using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Synchronizer.OrderAggregate
{
    public sealed class OrderQueryDbSynchronizer : IOrderQueryDbSynchronizer
    {
        private readonly OrderQueryDbContext _dbContext;

        public OrderQueryDbSynchronizer(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public Task ProjectCreatedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectRemarkedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectReservationChangedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectIssuedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectServicedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        private async Task UpsertAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken)
        {
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(row => row.Id == snapshot.OrderId, cancellationToken);

            if (order is null)
                _dbContext.Orders.Add(Apply(snapshot, new OrderReadModel { Id = snapshot.OrderId }));
            else if (snapshot.OccurredAt <= order.LastProjectedAt)
                return;
            else
                Apply(snapshot, order);

            await ReconcileAsync(_dbContext.OrderTravellers, snapshot, snapshot.Travellers, traveller => traveller.TravellerId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderTravellerDocuments, snapshot, Documents(snapshot), document => document.Document.DocumentId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderJourneys, snapshot, snapshot.Journeys, journey => journey.JourneyId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderSegments, snapshot, snapshot.Segments, segment => segment.SegmentId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderSegmentLegs, snapshot, Legs(snapshot), leg => leg.Leg.SegmentLegId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderItems, snapshot, snapshot.Items, item => item.ItemId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderServices, snapshot, snapshot.Services, service => service.ServiceId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderAncillaryServices, snapshot, Ancillaries(snapshot), ancillary => ancillary.ServiceId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderPricingLines, snapshot, snapshot.PricingLines, line => line.PricingLineId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderPricingAllocations, snapshot, Allocations(snapshot), allocation => allocation.Allocation.AllocationId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderContacts, snapshot, snapshot.Contacts, contact => contact.ContactId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderContactPoints, snapshot, ContactPoints(snapshot), point => point.ContactPoint.ContactPointId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderRemarks, snapshot, snapshot.Remarks, remark => remark.RemarkId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderChanges, snapshot, snapshot.Changes, change => change.ChangeId, Apply, cancellationToken);
        }

        private async Task ReconcileAsync<TReadModel, TSource>(
            DbSet<TReadModel> set,
            OrderReadModelSnapshot snapshot,
            IReadOnlyList<TSource> sources,
            Func<TSource, long> identify,
            Action<TSource, TReadModel> apply,
            CancellationToken cancellationToken)
            where TReadModel : class, IOrderOwnedReadModel, new()
        {
            var stored = await set
                .Where(row => row.OrderId == snapshot.OrderId)
                .ToListAsync(cancellationToken);
            var storedById = stored.ToDictionary(row => row.Id);

            foreach (var source in sources)
            {
                var id = identify(source);

                if (storedById.TryGetValue(id, out var row))
                {
                    apply(source, row);
                    continue;
                }

                row = new TReadModel { Id = id, OrderId = snapshot.OrderId };
                apply(source, row);
                set.Add(row);
            }

            var incomingIds = sources.Select(identify).ToHashSet();

            foreach (var row in stored.Where(row => !incomingIds.Contains(row.Id)))
                set.Remove(row);
        }

        private static IReadOnlyList<OwnedDocument> Documents(OrderReadModelSnapshot snapshot)
            => snapshot.Travellers
                .SelectMany(traveller => traveller.Documents.Select(document => new OwnedDocument(traveller.TravellerId, document)))
                .ToList();

        private static IReadOnlyList<OwnedAncillary> Ancillaries(OrderReadModelSnapshot snapshot)
            => snapshot.Services
                .Where(service => service.Ancillary is not null)
                .Select(service => new OwnedAncillary(service.ServiceId, service.Ancillary!))
                .ToList();

        private static IReadOnlyList<OwnedLeg> Legs(OrderReadModelSnapshot snapshot)
            => snapshot.Segments
                .SelectMany(segment => segment.Legs.Select(leg => new OwnedLeg(segment.SegmentId, leg)))
                .ToList();

        private static IReadOnlyList<OwnedAllocation> Allocations(OrderReadModelSnapshot snapshot)
            => snapshot.PricingLines
                .SelectMany(line => line.Allocations.Select(allocation => new OwnedAllocation(line.PricingLineId, allocation)))
                .ToList();

        private static IReadOnlyList<OwnedContactPoint> ContactPoints(OrderReadModelSnapshot snapshot)
            => snapshot.Contacts
                .SelectMany(contact => contact.ContactPoints.Select(point => new OwnedContactPoint(contact.ContactId, point)))
                .ToList();

        private static OrderReadModel Apply(OrderReadModelSnapshot snapshot, OrderReadModel target)
        {
            target.OrderReference = snapshot.OrderReference;
            target.RecordLocator = snapshot.RecordLocator;
            target.SourceOfferId = snapshot.SourceOfferId;
            target.Status = snapshot.Status;
            target.Channel = snapshot.Channel;
            target.CustomerId = snapshot.CustomerId;
            target.ActorId = snapshot.ActorId;
            target.TravelAgencyId = snapshot.TravelAgencyId;
            target.OfficeKind = snapshot.OfficeKind;
            target.OfficeId = snapshot.OfficeId;
            target.CurrencyId = snapshot.CurrencyId;
            target.CustomerTotal = snapshot.CustomerTotal;
            target.CommercialVersion = snapshot.CommercialVersion;
            target.LastTicketingDate = snapshot.LastTicketingDate;
            target.CreatedAt = snapshot.CreatedAt;
            target.LastProjectedAt = snapshot.OccurredAt;
            return target;
        }

        private static void Apply(OrderTravellerSnapshot source, OrderTravellerReadModel target)
        {
            target.Index = source.Index;
            target.SourceTravellerRef = source.SourceTravellerRef;
            target.PassengerType = source.PassengerType;
            target.AgeRange = source.AgeRange;
            target.InfantParentTravellerId = source.InfantParentTravellerId;
            target.Status = source.Status;
            target.GivenName = source.GivenName;
            target.Surname = source.Surname;
            target.DateOfBirth = source.DateOfBirth;
            target.Gender = source.Gender;
            target.NationalityId = source.NationalityId;
            target.CountryOfResidenceId = source.CountryOfResidenceId;
        }

        private static void Apply(OwnedDocument source, OrderTravellerDocumentReadModel target)
        {
            target.TravellerId = source.TravellerId;
            target.Type = source.Document.Type;
            target.Number = source.Document.Number;
            target.ExpiryDate = source.Document.ExpiryDate;
            target.IssuanceCountryId = source.Document.IssuanceCountryId;
            target.Holder = source.Document.Holder;
        }

        private static void Apply(OrderJourneySnapshot source, OrderJourneyReadModel target)
        {
            target.Sequence = source.Sequence;
            target.BoundId = source.BoundId;
            target.OriginAirportId = source.OriginAirportId;
            target.DestinationAirportId = source.DestinationAirportId;
        }

        private static void Apply(OrderSegmentSnapshot source, OrderSegmentReadModel target)
        {
            target.OrderJourneyId = source.JourneyId;
            target.Sequence = source.Sequence;
            target.FlightId = source.FlightId;
            target.FlightVersion = source.FlightVersion;
            target.FlightNumber = source.FlightNumber;
            target.MarketingAirlineId = source.MarketingAirlineId;
            target.OperatingAirlineId = source.OperatingAirlineId;
            target.OriginAirportId = source.OriginAirportId;
            target.OriginAirportTerminalId = source.OriginAirportTerminalId;
            target.DestinationAirportId = source.DestinationAirportId;
            target.DestinationAirportTerminalId = source.DestinationAirportTerminalId;
            target.SoldDeparture = source.SoldDeparture;
            target.SoldArrival = source.SoldArrival;
            target.Duration = source.Duration;
            target.AircraftId = source.AircraftId;
        }

        private static void Apply(OwnedLeg source, OrderSegmentLegReadModel target)
        {
            target.OrderSegmentId = source.SegmentId;
            target.Sequence = source.Leg.Sequence;
            target.LegId = source.Leg.LegId;
            target.OriginAirportId = source.Leg.OriginAirportId;
            target.DestinationAirportId = source.Leg.DestinationAirportId;
            target.DepartureDateTime = source.Leg.DepartureDateTime;
            target.ArrivalDateTime = source.Leg.ArrivalDateTime;
        }

        private static void Apply(OrderItemSnapshot source, OrderItemReadModel target)
        {
            target.Kind = source.Kind;
            target.AcceptedTotal = source.AcceptedTotal;
            target.CommercialStatus = source.CommercialStatus;
            target.EndedByChangeId = source.EndedByChangeId;
        }

        private static void Apply(OrderServiceSnapshot source, OrderServiceReadModel target)
        {
            target.OrderItemId = source.ItemId;
            target.TravellerId = source.TravellerId;
            target.SegmentId = source.SegmentId;
            target.ServiceType = source.ServiceType;
            target.CommercialStatus = source.CommercialStatus;
            target.FlightCapacityId = source.FlightCapacityId;
            target.AirFareId = source.AirFareId;
            target.BookingClass = source.BookingClass;
            target.FareBasis = source.FareBasis;
            target.FareFamily = source.FareFamily;
            target.FareType = source.FareType;
            target.RbdId = source.RbdId;
            target.CabinClassId = source.CabinClassId;
            target.CheckedBaggagePieces = source.CheckedBaggage?.Pieces;
            target.CheckedBaggageWeight = source.CheckedBaggage?.Weight;
            target.CheckedBaggageUnit = source.CheckedBaggage?.Unit;
            target.CabinBaggagePieces = source.CabinBaggage?.Pieces;
            target.CabinBaggageWeight = source.CabinBaggage?.Weight;
            target.CabinBaggageUnit = source.CabinBaggage?.Unit;
            target.IsRefundable = source.IsRefundable;
            target.IsChangeable = source.IsChangeable;
            target.IsUpgradable = source.IsUpgradable;
            target.SeatNumber = source.SeatNumber;
            target.EndedByChangeId = source.EndedByChangeId;
        }

        private static void Apply(OwnedAncillary source, OrderAncillaryServiceReadModel target)
        {
            target.VariantCode = source.Ancillary.VariantCode;
            target.Profile = source.Ancillary.Profile;
            target.Quantity = source.Ancillary.Quantity;
            target.SelectionScope = source.Ancillary.SelectionScope;
            target.CoverageScope = source.Ancillary.CoverageScope;
            target.CoveredAirServiceIds = source.Ancillary.CoveredAirServiceIds.ToList();
            target.PointOfSaleId = source.Ancillary.PointOfSaleId;
            target.SourceKind = source.Ancillary.Source.Kind;
            target.SourceReference = source.Ancillary.Source.Reference;
            target.SourceOrderCommercialVersion = source.Ancillary.Source.OrderCommercialVersion;
            target.SourceSelectionId = source.Ancillary.Source.SelectionId;
            target.SourceOfferItemId = source.Ancillary.Source.OfferItemId;
            target.DefinitionVersionId = source.Ancillary.Definition.DefinitionVersionId;
            target.ServiceDefinitionRef = source.Ancillary.Definition.ServiceDefinitionRef;
            target.ServiceTypeCode = source.Ancillary.Definition.ServiceTypeCode;
            target.ServiceSubCode = source.Ancillary.Definition.ServiceSubCode;
            target.Rfic = source.Ancillary.Definition.Rfic;
            target.Rfisc = source.Ancillary.Definition.Rfisc;
            target.SsrCode = source.Ancillary.Definition.SsrCode;
            target.ProvisionId = source.Ancillary.Provision.ProvisionId;
            target.PurchaseStage = source.Ancillary.Provision.PurchaseStage;
            target.BookingMethod = source.Ancillary.Provision.BookingMethod;
            target.BookingRequired = source.Ancillary.Provision.BookingRequired;
            target.ConfirmationRequirement = source.Ancillary.Provision.ConfirmationRequirement;
            target.ProvisionFulfillmentProviderKey = source.Ancillary.Provision.SourceFulfillmentProviderKey;
            target.DocumentRouting = source.Ancillary.Provision.DocumentRouting;
            target.DocumentType = source.Ancillary.Provision.DocumentType;
            target.DocumentRequired = source.Ancillary.Provision.DocumentRequired;
            target.RequiresCheckAtReserve = source.Ancillary.Provision.RequiresCheckAtReserve;
            target.PricingId = source.Ancillary.Pricing.PricingId;
            target.PricingRateId = source.Ancillary.Pricing.PricingRateId;
            target.PricingVersion = source.Ancillary.Pricing.PricingVersion;
            target.PricingUnit = source.Ancillary.Pricing.PricingUnit;
            target.BaggageChargeKind = source.Ancillary.Baggage?.ChargeKind;
            target.BaggageAllowanceConcept = source.Ancillary.Baggage?.AllowanceConcept;
            target.BaggagePackageWeightKg = source.Ancillary.Baggage?.PackageWeightKg;
            target.BaggageWeightFromExclusiveKg = source.Ancillary.Baggage?.WeightFromExclusiveKg;
            target.BaggageWeightToInclusiveKg = source.Ancillary.Baggage?.WeightToInclusiveKg;
            target.BaggageEquipmentKind = source.Ancillary.Baggage?.EquipmentKind;
            target.BaggageBagRef = source.Ancillary.Baggage?.BagRef;
            target.BaggageMeasuredWeightKg = source.Ancillary.Baggage?.MeasuredWeightKg;
            target.BaggageLengthCm = source.Ancillary.Baggage?.Dimensions?.LengthCm;
            target.BaggageWidthCm = source.Ancillary.Baggage?.Dimensions?.WidthCm;
            target.BaggageHeightCm = source.Ancillary.Baggage?.Dimensions?.HeightCm;
            target.BaggageWeightKg = source.Ancillary.Baggage?.WeightKg;
            target.MealKind = source.Ancillary.Meal?.MealKind;
            target.MealCode = source.Ancillary.Meal?.MealCode;
            target.MealMenuItemRef = source.Ancillary.Meal?.MenuItemRef;
            target.MealDietaryCode = source.Ancillary.Meal?.DietaryCode;
            target.MealExclusiveFamilyCode = source.Ancillary.Meal?.ExclusiveMealFamilyCode;
            target.PetTransportMode = source.Ancillary.Pet?.TransportMode;
            target.PetAnimalType = source.Ancillary.Pet?.AnimalType;
            target.PetOtherAnimalCode = source.Ancillary.Pet?.OtherAnimalCode;
            target.PetAnimalAgeWeeks = source.Ancillary.Pet?.AnimalAgeWeeks;
            target.PetCombinedWeightKg = source.Ancillary.Pet?.CombinedWeightKg;
            target.PetCarrierLengthCm = source.Ancillary.Pet?.CarrierDimensions.LengthCm;
            target.PetCarrierWidthCm = source.Ancillary.Pet?.CarrierDimensions.WidthCm;
            target.PetCarrierHeightCm = source.Ancillary.Pet?.CarrierDimensions.HeightCm;
            target.PetDocumentAcknowledgements = source.Ancillary.Pet?.DocumentAcknowledgements.ToList() ?? [];
            target.PetSizeBracket = source.Ancillary.Pet?.SizeBracket;
            target.AssistanceKind = source.Ancillary.Assistance?.AssistanceKind;
            target.AssistanceSsrCode = source.Ancillary.Assistance?.AssistanceSsrCode;
            target.AssistanceInfantTravellerId = source.Ancillary.Assistance?.InfantTravellerId;
            target.AssistanceGuardianTravellerId = source.Ancillary.Assistance?.GuardianTravellerId;
            target.AirportServiceKind = source.Ancillary.AirportService?.Kind;
            target.AirportServiceAirportId = source.Ancillary.AirportService?.AirportId;
            target.AirportServiceTerminalRef = source.Ancillary.AirportService?.TerminalRef;
            target.AirportServiceFacilityId = source.Ancillary.AirportService?.FacilityId;
            target.AirportServiceDirection = source.Ancillary.AirportService?.Direction;
            target.AirportServiceTimeWithOffset = source.Ancillary.AirportService?.TimeWithOffset;
            target.AirportServiceGuestCount = source.Ancillary.AirportService?.GuestCount;
            target.PriorityKind = source.Ancillary.Priority?.Kind;
            target.PriorityZoneCode = source.Ancillary.Priority?.PriorityZoneCode;
            target.PriorityGroupCode = source.Ancillary.Priority?.PriorityGroupCode;
            target.PriorityFareBenefitRef = source.Ancillary.Priority?.FareBenefitRef;
            target.PriorityOptIn = source.Ancillary.Priority?.OptIn;
            target.ConnectivityPlanKind = source.Ancillary.Connectivity?.PlanKind;
            target.ConnectivityDurationMinutes = source.Ancillary.Connectivity?.DurationMinutes;
            target.ConnectivityIncludedDataMb = source.Ancillary.Connectivity?.IncludedDataMb;
            target.ConnectivityMaxDevices = source.Ancillary.Connectivity?.MaxDevices;
            target.ConnectivityDeliveryStage = source.Ancillary.Connectivity?.DeliveryStage;
            target.ConnectivityDeviceCount = source.Ancillary.Connectivity?.DeviceCount;
            target.UpgradeFromCabinId = source.Ancillary.Upgrade?.FromCabinId;
            target.UpgradeToCabinId = source.Ancillary.Upgrade?.ToCabinId;
            target.UpgradeKind = source.Ancillary.Upgrade?.UpgradeKind;
            target.UpgradeRequiresTicketExchange = source.Ancillary.Upgrade?.RequiresTicketExchange;
        }

        private static void Apply(OrderPricingLineSnapshot source, OrderPricingLineReadModel target)
        {
            target.Reason = source.Reason;
            target.Scope = source.Scope;
            target.Category = source.Category;
            target.SubCategory = source.SubCategory;
            target.Direction = source.Direction;
            target.Treatment = source.Treatment;
            target.Code = source.Code;
            target.Description = source.Description;
            target.Reference = source.Reference;
            target.Amount = source.Amount;
            target.CurrencyId = source.CurrencyId;
            target.EquivalentAmount = source.EquivalentAmount;
            target.EquivalentCurrencyId = source.EquivalentCurrencyId;
            target.Refundability = source.Refundability;
        }

        private static void Apply(OwnedAllocation source, OrderPricingAllocationReadModel target)
        {
            target.PricingLineId = source.PricingLineId;
            target.OrderItemId = source.Allocation.ItemId;
            target.OrderServiceId = source.Allocation.ServiceId;
            target.OrderJourneyId = source.Allocation.JourneyId;
            target.OrderSegmentId = source.Allocation.SegmentId;
            target.TravellerId = source.Allocation.TravellerId;
            target.Amount = source.Allocation.Amount;
            target.CurrencyId = source.Allocation.CurrencyId;
            target.EquivalentAmount = source.Allocation.EquivalentAmount;
            target.EquivalentCurrencyId = source.Allocation.EquivalentCurrencyId;
        }

        private static void Apply(OrderContactSnapshot source, OrderContactReadModel target)
        {
            target.Sequence = source.Sequence;
            target.Role = source.Role;
            target.ContactName = source.ContactName;
        }

        private static void Apply(OwnedContactPoint source, OrderContactPointReadModel target)
        {
            target.OrderContactId = source.ContactId;
            target.Type = source.ContactPoint.Type;
            target.Value = source.ContactPoint.Value;
            target.CountryCode = source.ContactPoint.CountryCode;
            target.IsPrimary = source.ContactPoint.IsPrimary;
        }

        private static void Apply(OrderRemarkSnapshot source, OrderRemarkReadModel target)
        {
            target.Type = source.Type;
            target.Visibility = source.Visibility;
            target.Scope = source.Scope;
            target.TravellerId = source.TravellerId;
            target.SegmentId = source.SegmentId;
            target.OrderItemId = source.ItemId;
            target.OrderServiceId = source.ServiceId;
            target.Text = source.Text;
            target.CategoryCode = source.CategoryCode;
            target.IsPrintedOnItinerary = source.IsPrintedOnItinerary;
            target.IsPrintedOnInvoice = source.IsPrintedOnInvoice;
            target.Status = source.Status;
            target.SupersedesRemarkId = source.SupersedesRemarkId;
            target.CreatedBy = source.CreatedBy;
            target.CreatedAt = source.CreatedAt;
        }

        private static void Apply(OrderChangeSnapshot source, OrderChangeReadModel target)
        {
            target.ChangeType = source.ChangeType;
            target.CommercialVersion = source.CommercialVersion;
            target.ActorId = source.ActorId;
            target.Channel = source.Channel;
            target.ContextType = source.ContextType;
            target.PrincipalType = source.PrincipalType;
            target.SourceReference = source.SourceReference;
            target.SourceSystem = source.SourceSystem;
            target.ReasonCode = source.ReasonCode;
            target.ReasonText = source.ReasonText;
            target.IsInvoluntary = source.IsInvoluntary;
            target.WaiverCode = source.WaiverCode;
            target.CommittedAt = source.CommittedAt;
        }

        private sealed record OwnedDocument(long TravellerId, OrderTravellerDocumentSnapshot Document);

        private sealed record OwnedLeg(long SegmentId, OrderSegmentLegSnapshot Leg);

        private sealed record OwnedAncillary(long ServiceId, OrderAncillarySnapshot Ancillary);

        private sealed record OwnedAllocation(long PricingLineId, OrderPricingAllocationSnapshot Allocation);

        private sealed record OwnedContactPoint(long ContactId, OrderContactPointSnapshot ContactPoint);
    }
}
