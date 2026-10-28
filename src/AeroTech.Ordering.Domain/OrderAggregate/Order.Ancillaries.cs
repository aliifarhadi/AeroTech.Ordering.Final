using System.Globalization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        private const string FlightRefSeparator = ":F";

        public bool AcceptsAncillariesAt(DateTimeOffset now, bool hasFulfillmentHistory)
            => Status == OrderStatus.Created && !hasFulfillmentHistory && !HasPassedLastTicketingDateAt(now);

        public OrderChange AddAncillaries(
            ResolvedAncillarySelection? selection,
            ResolvedSeatQuote? seats,
            int expectedCommercialVersion,
            bool hasFulfillmentHistory,
            SalesContext actorContext,
            IIdGenerator idGenerator,
            DateTimeOffset now)
        {
            EnsureAcceptsAncillaries(expectedCommercialVersion, hasFulfillmentHistory, now);

            if (selection is null && seats is null)
                throw ExceptionFactory.AncillaryBasketIsEmpty(Id);

            var sourceReference = Id.ToString(CultureInfo.InvariantCulture);

            if (selection is not null)
                EnsureSelectionIsCurrent(selection, ShoppingSourceKind.Order, sourceReference, now);

            if (seats is not null)
                EnsureSeatQuoteIsCurrent(seats, ShoppingSourceKind.Order, sourceReference, now);

            var changeId = idGenerator.NewId();
            var purchases = new List<PlannedPurchase>();

            if (selection is not null)
                PlanAncillaries(selection, changeId, OrderPricingReason.AdditionalSale, idGenerator, now, purchases);

            if (seats is not null)
                PlanSeats(seats, changeId, OrderPricingReason.AdditionalSale, idGenerator, now, purchases);

            var change = new OrderChange(
                changeId,
                Id,
                OrderChangeType.AddProduct,
                CommercialVersion + 1,
                actorContext.Copy(),
                selection?.SelectionId,
                null,
                null,
                null,
                false,
                null,
                now);

            CommercialVersion = change.CommercialVersion;
            _changes.Add(change);

            Commit(purchases);
            ReconcileTotals();

            return change;
        }

        public void EnsureFulfillmentIsSupported()
        {
            var unsupported = _services.FirstOrDefault(service => service.IsActive && service is OrderAncillaryService or OrderSeatService { IsPricedSelection: true });

            if (unsupported is not null)
                throw ExceptionFactory.AncillaryServiceIsNotFulfillable(Id, unsupported.Id);
        }

        public void EnsureAcceptsAncillaries(int expectedCommercialVersion, bool hasFulfillmentHistory, DateTimeOffset now)
        {
            if (Status != OrderStatus.Created)
                throw ExceptionFactory.OrderDoesNotAcceptAncillariesInStatus(Id, Status);

            if (CommercialVersion != expectedCommercialVersion)
                throw ExceptionFactory.OrderCommercialVersionIsNotExpected(Id, CommercialVersion, expectedCommercialVersion);

            if (hasFulfillmentHistory)
                throw ExceptionFactory.OrderHasFulfillmentHistory(Id);

            if (HasPassedLastTicketingDateAt(now))
                throw ExceptionFactory.OrderDoesNotAcceptAncillariesAfterLastTicketingDate(Id, LastTicketingDate);
        }

        private void EnsureSelectionIsCurrent(
            ResolvedAncillarySelection selection,
            ShoppingSourceKind sourceKind,
            string sourceReference,
            DateTimeOffset now)
        {
            EnsureSourceIsCurrent(
                selection.SelectionId,
                selection.SourceKind,
                selection.SourceReference,
                selection.OrderCommercialVersion,
                selection.ExpiresAt,
                sourceKind,
                sourceReference,
                now);

            if (selection.Lines.Count == 0)
                throw ExceptionFactory.AncillarySelectionIsEmpty(selection.SelectionId);

            if (selection.SelectedLineCount != selection.Lines.Count)
                throw ExceptionFactory.AncillarySelectionLineCountMismatch(selection.SelectionId, selection.SelectedLineCount, selection.Lines.Count);

            if (selection.Lines.GroupBy(line => line.OfferItemId, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } duplicate)
                throw ExceptionFactory.AncillaryOfferItemIsDuplicated(selection.SelectionId, duplicate.Key);
        }

        private void EnsureSeatQuoteIsCurrent(
            ResolvedSeatQuote quote,
            ShoppingSourceKind sourceKind,
            string sourceReference,
            DateTimeOffset now)
        {
            var quoteReference = string.Join(",", quote.Seats.Select(seat => seat.SeatSelectionId));

            EnsureSourceIsCurrent(
                quoteReference,
                quote.SourceKind,
                quote.SourceReference,
                quote.OrderCommercialVersion,
                quote.ExpiresAt,
                sourceKind,
                sourceReference,
                now);

            if (quote.Seats.Count == 0)
                throw ExceptionFactory.AncillaryBasketIsEmpty(Id);

            if (quote.Seats.GroupBy(seat => seat.SeatSelectionId, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } duplicate)
                throw ExceptionFactory.AncillaryOfferItemIsDuplicated(quoteReference, duplicate.Key);
        }

        private void EnsureSourceIsCurrent(
            string selectionReference,
            ShoppingSourceKind actualKind,
            string actualReference,
            int? actualCommercialVersion,
            DateTimeOffset expiresAt,
            ShoppingSourceKind sourceKind,
            string sourceReference,
            DateTimeOffset now)
        {
            if (actualKind != sourceKind || !string.Equals(actualReference, sourceReference, StringComparison.Ordinal))
                throw ExceptionFactory.AncillarySelectionBelongsToAnotherSource(selectionReference, actualKind, actualReference);

            if (sourceKind == ShoppingSourceKind.Order && actualCommercialVersion != CommercialVersion)
                throw ExceptionFactory.AncillarySelectionVersionIsStale(selectionReference, actualCommercialVersion, Id, CommercialVersion);

            if (expiresAt <= now)
                throw ExceptionFactory.AncillarySelectionHasExpired(selectionReference, expiresAt);
        }

        private void Commit(IReadOnlyList<PlannedPurchase> purchases)
        {
            foreach (var purchase in purchases)
            {
                _items.Add(purchase.Item);
                _services.Add(purchase.Service);
                _pricingLines.AddRange(purchase.PricingLines);
            }
        }

        private void PlanSeats(
            ResolvedSeatQuote quote,
            long changeId,
            OrderPricingReason reason,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt,
            List<PlannedPurchase> purchases)
        {
            foreach (var seat in quote.Seats)
            {
                var variant = AncillaryVariant.Find(seat.VariantCode)
                              ?? throw ExceptionFactory.AncillaryVariantIsNotRecognised(seat.SeatSelectionId, seat.VariantCode);

                if (!variant.IsSeatAssignment || !string.Equals(seat.Definition.VariantCode, variant.Code, StringComparison.Ordinal))
                    throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(seat.SeatSelectionId, seat.VariantCode, AncillaryProfile.Seat);

                if (seat.Pricing.Quantity != 1)
                    throw ExceptionFactory.AncillaryQuantityIsInvalid(seat.SeatSelectionId, 1, seat.Pricing.Quantity);

                if (string.IsNullOrWhiteSpace(seat.SeatNumber))
                    throw ExceptionFactory.AncillaryChoiceIsMissing(seat.SeatSelectionId, seat.VariantCode, nameof(seat.SeatNumber));

                if (seat.PointOfSaleId <= 0)
                    throw ExceptionFactory.AncillarySourceContractIsIncomplete(seat.SeatSelectionId, nameof(seat.PointOfSaleId));

                var traveller = TravellerOf(seat.SeatSelectionId, seat.TravellerRef, quote.SourceKind);
                var segments = _segments.Where(segment => segment.FlightId == seat.FlightId).ToList();

                var airService = segments.Count == 1 && ActiveAirServiceOf(traveller.Id, segments[0].Id) is { } match
                    ? match
                    : throw ExceptionFactory.AncillaryFlightIsNotInOrder(seat.SeatSelectionId, seat.FlightId);

                var seatNumber = seat.SeatNumber.Trim();

                EnsureSeatIsFree(seat.SeatSelectionId, seatNumber, airService, purchases);

                var components = AcceptedPriceComponents(seat.SeatSelectionId, seat.Pricing);
                var item = new OrderItem(idGenerator.NewId(), Id, variant.ProductType, changeId, createdAt);

                var service = new OrderSeatService(
                    idGenerator.NewId(),
                    Id,
                    item.Id,
                    traveller.Id,
                    airService.SegmentId,
                    airService.Id,
                    seatNumber,
                    variant,
                    seat.PointOfSaleId,
                    new AncillarySaleSource(quote.SourceKind, quote.SourceReference, quote.OrderCommercialVersion, seat.SeatSelectionId, null),
                    DefinitionOf(seat.Definition, seat.SeatSelectionId),
                    ProvisionOf(seat.Provision, seat.SeatSelectionId),
                    new AncillaryPricing(seat.Pricing.PricingId, seat.Pricing.PricingRateId, seat.Pricing.PricingVersion, seat.Pricing.PricingUnit),
                    changeId,
                    createdAt);

                purchases.Add(new PlannedPurchase(item, service, PricingLinesFor(components, item, service, [airService], reason, idGenerator, changeId, createdAt)));
            }
        }

        private void EnsureSeatIsFree(string seatSelectionId, string seatNumber, OrderAirTransportService airService, IReadOnlyList<PlannedPurchase> purchases)
        {
            var segmentSeats = _services
                .Concat(purchases.Select(purchase => purchase.Service))
                .OfType<OrderSeatService>()
                .Where(existing => existing.IsActive && existing.SegmentId == airService.SegmentId)
                .ToList();

            if (segmentSeats.Any(existing => existing.AssociatedAirServiceId == airService.Id))
                throw ExceptionFactory.AncillarySeatIsAlreadyAssigned(seatSelectionId, airService.TravellerId, airService.SegmentId);

            if (segmentSeats.Any(existing => string.Equals(existing.SeatNumber, seatNumber, StringComparison.OrdinalIgnoreCase)))
                throw ExceptionFactory.AncillarySeatIsTaken(seatSelectionId, seatNumber, airService.SegmentId);
        }

        private void PlanAncillaries(
            ResolvedAncillarySelection selection,
            long changeId,
            OrderPricingReason reason,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt,
            List<PlannedPurchase> purchases)
        {
            foreach (var line in selection.Lines)
            {
                var variant = VariantOf(line);

                if (line.SelectedQuantity <= 0 || line.SelectedQuantity != line.Pricing.Quantity)
                    throw ExceptionFactory.AncillaryQuantityIsInvalid(line.OfferItemId, line.SelectedQuantity, line.Pricing.Quantity);

                if (!OrderAncillaryService.ScopesAgree(line.SelectionScope, line.Provision.CoverageScope))
                    throw ExceptionFactory.AncillaryScopeIsInconsistent(line.OfferItemId, line.SelectionScope, line.Provision.CoverageScope);

                var (traveller, coveredAirServices) = CoverageOf(line, selection.SourceKind);
                var components = AcceptedPriceComponents(line.OfferItemId, line.Pricing);

                var item = new OrderItem(idGenerator.NewId(), Id, variant.ProductType, changeId, createdAt);

                var service = NewAncillaryService(
                    line,
                    variant,
                    item.Id,
                    traveller?.Id,
                    coveredAirServices.Select(airService => airService.Id).ToList(),
                    SourceOf(selection, line),
                    selection.SourceKind,
                    idGenerator.NewId(),
                    changeId,
                    createdAt);

                purchases.Add(new PlannedPurchase(item, service, PricingLinesFor(components, item, service, coveredAirServices, reason, idGenerator, changeId, createdAt)));
            }
        }

        private static AncillaryVariant VariantOf(ResolvedAncillaryLine line)
        {
            var variant = AncillaryVariant.Find(line.VariantCode)
                          ?? throw ExceptionFactory.AncillaryVariantIsNotRecognised(line.OfferItemId, line.VariantCode);

            if (variant.Profile != line.Profile || variant.IsSeatAssignment || !string.Equals(line.Definition.VariantCode, variant.Code, StringComparison.Ordinal))
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(line.OfferItemId, line.VariantCode, line.Profile);

            return variant;
        }

        private static AncillarySaleSource SourceOf(ResolvedAncillarySelection selection, ResolvedAncillaryLine line)
            => new(selection.SourceKind, selection.SourceReference, selection.OrderCommercialVersion, selection.SelectionId, line.OfferItemId);

        private (OrderTraveller? Traveller, IReadOnlyList<OrderAirTransportService> AirServices) CoverageOf(
            ResolvedAncillaryLine line,
            ShoppingSourceKind sourceKind)
        {
            var unit = line.Unit;
            var coverage = line.Coverage;

            if (line.SelectionScope == SelectionScope.PerOrder)
            {
                if (unit.TravellerRef is not null || unit.PortionRef is not null || unit.FlightRef is not null
                    || coverage.TravellerRefs.Count > 0 || coverage.PortionRefs.Count > 0)
                    throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

                if (coverage.FlightRefs.Count > 0)
                    throw ExceptionFactory.AncillaryCoverageIsNotSupported(line.OfferItemId, line.Provision.CoverageScope);

                return (null, []);
            }

            if (unit.TravellerRef is null || !coverage.TravellerRefs.SequenceEqual([unit.TravellerRef], StringComparer.Ordinal))
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            var traveller = TravellerOf(line.OfferItemId, unit.TravellerRef, sourceKind);

            return line.SelectionScope switch
            {
                SelectionScope.PerTravellerPerFlight => (traveller, [SectorAirServiceOf(line, traveller)]),
                SelectionScope.PerTravellerPerPortion => (traveller, PortionAirServicesOf(line, traveller)),
                SelectionScope.PerTraveller => (traveller, JourneyAirServicesOf(line, traveller)),
                _ => throw ExceptionFactory.AncillaryScopeIsInconsistent(line.OfferItemId, line.SelectionScope, line.Provision.CoverageScope)
            };
        }

        private OrderAirTransportService SectorAirServiceOf(ResolvedAncillaryLine line, OrderTraveller traveller)
        {
            var flightRef = line.Unit.FlightRef;

            if (flightRef is null || line.Unit.PortionRef is not null || line.Coverage.PortionRefs.Count > 0
                || !line.Coverage.FlightRefs.SequenceEqual([flightRef], StringComparer.Ordinal))
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            return FlightAirServiceOf(line.OfferItemId, traveller, flightRef);
        }

        private IReadOnlyList<OrderAirTransportService> PortionAirServicesOf(ResolvedAncillaryLine line, OrderTraveller traveller)
        {
            var portionRef = line.Unit.PortionRef;

            if (portionRef is null || line.Unit.FlightRef is not null || line.Coverage.FlightRefs.Count > 0
                || !line.Coverage.PortionRefs.SequenceEqual([portionRef], StringComparer.Ordinal))
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            var journey = _journeys.SingleOrDefault(item => string.Equals(item.BoundId, portionRef, StringComparison.OrdinalIgnoreCase))
                          ?? throw ExceptionFactory.AncillaryPortionIsNotInOrder(line.OfferItemId, portionRef);

            var airServices = _segments
                .Where(segment => segment.OrderJourneyId == journey.Id)
                .OrderBy(segment => segment.Sequence)
                .Select(segment => ActiveAirServiceOf(traveller.Id, segment.Id))
                .OfType<OrderAirTransportService>()
                .ToList();

            return airServices.Count > 0
                ? airServices
                : throw ExceptionFactory.AncillaryPortionIsNotInOrder(line.OfferItemId, portionRef);
        }

        private IReadOnlyList<OrderAirTransportService> JourneyAirServicesOf(ResolvedAncillaryLine line, OrderTraveller traveller)
        {
            if (line.Unit.PortionRef is not null || line.Unit.FlightRef is not null || line.Coverage.PortionRefs.Count > 0)
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            if (line.Coverage.FlightRefs.Count == 0)
                throw ExceptionFactory.AncillaryCoverageIsNotSupported(line.OfferItemId, line.Provision.CoverageScope);

            var airServices = line.Coverage.FlightRefs
                .Select(flightRef => FlightAirServiceOf(line.OfferItemId, traveller, flightRef))
                .ToList();

            return airServices.DistinctBy(airService => airService.Id).Count() == airServices.Count
                ? airServices
                : throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);
        }

        private OrderAirTransportService FlightAirServiceOf(string reference, OrderTraveller traveller, string flightRef)
        {
            var separator = flightRef.LastIndexOf(FlightRefSeparator, StringComparison.Ordinal);

            if (separator <= 0 || !int.TryParse(flightRef[(separator + FlightRefSeparator.Length)..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
                throw ExceptionFactory.AncillaryFlightIsNotInOrder(reference, flightRef);

            var boundId = flightRef[..separator];
            var segments = _journeys
                .Where(journey => string.Equals(journey.BoundId, boundId, StringComparison.OrdinalIgnoreCase))
                .SelectMany(journey => _segments.Where(segment => segment.OrderJourneyId == journey.Id && segment.Sequence == sequence))
                .ToList();

            return segments.Count == 1 && ActiveAirServiceOf(traveller.Id, segments[0].Id) is { } airService
                ? airService
                : throw ExceptionFactory.AncillaryFlightIsNotInOrder(reference, flightRef);
        }

        private OrderAirTransportService? ActiveAirServiceOf(long travellerId, long segmentId)
            => _services
                .OfType<OrderAirTransportService>()
                .SingleOrDefault(service => service.IsActive && service.TravellerId == travellerId && service.SegmentId == segmentId);

        private OrderTraveller TravellerOf(string reference, string travellerRef, ShoppingSourceKind sourceKind)
        {
            var traveller = sourceKind == ShoppingSourceKind.Order
                ? _travellers.SingleOrDefault(item => string.Equals(item.Id.ToString(CultureInfo.InvariantCulture), travellerRef, StringComparison.Ordinal))
                : _travellers.SingleOrDefault(item => string.Equals(item.SourceTravellerRef, travellerRef, StringComparison.OrdinalIgnoreCase));

            return traveller is { Status: OrderTravellerStatus.Active }
                ? traveller
                : throw ExceptionFactory.AncillaryTravellerIsNotInOrder(reference, travellerRef);
        }

        private OrderAncillaryService NewAncillaryService(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            long orderItemId,
            long? travellerId,
            IReadOnlyList<long> coveredAirServiceIds,
            AncillarySaleSource source,
            ShoppingSourceKind sourceKind,
            long serviceId,
            long changeId,
            DateTimeOffset createdAt)
        {
            var definition = DefinitionOf(line.Definition, source.SelectionId);
            var provision = ProvisionOf(line.Provision, source.SelectionId);
            var pricing = new AncillaryPricing(line.Pricing.PricingId, line.Pricing.PricingRateId, line.Pricing.PricingVersion, line.Pricing.PricingUnit);
            var details = line.Details;

            if (variant.Profile == AncillaryProfile.Baggage)
                return new OrderBaggageService(
                    serviceId,
                    Id,
                    orderItemId,
                    travellerId,
                    variant,
                    line.SelectedQuantity,
                    line.SelectionScope,
                    line.Provision.CoverageScope,
                    coveredAirServiceIds,
                    source,
                    definition,
                    provision,
                    pricing,
                    BaggageChoiceOf(line, variant),
                    changeId,
                    createdAt);

            return new OrderAncillaryService(
                serviceId,
                Id,
                orderItemId,
                travellerId,
                variant,
                line.SelectedQuantity,
                line.SelectionScope,
                line.Provision.CoverageScope,
                coveredAirServiceIds,
                source,
                definition,
                provision,
                pricing,
                details.Meal is null ? null : MealChoiceOf(line, variant, details.Meal),
                details.Pet is null ? null : PetChoiceOf(line, variant, details.Pet),
                details.Assistance is null ? null : AssistanceChoiceOf(line, variant, details.Assistance, sourceKind),
                details.Assistance?.MedicalEquipment is null ? null : MedicalEquipmentChoiceOf(line, variant, details.Assistance.MedicalEquipment),
                details.Assistance?.UnaccompaniedMinor is null ? null : UnaccompaniedMinorChoiceOf(line, variant, details.Assistance.UnaccompaniedMinor),
                details.AirportService is null ? null : AirportServiceChoiceOf(line, variant, details.AirportService),
                details.Priority is null ? null : PriorityChoiceOf(line, variant, details.Priority),
                details.Connectivity is null ? null : ConnectivityChoiceOf(line, variant, details.Connectivity),
                details.Upgrade is null ? null : UpgradeChoiceOf(line, details.Upgrade),
                details.ExtraSeat is null ? null : ExtraSeatChoiceOf(line, variant, details.ExtraSeat),
                changeId,
                createdAt);
        }

        private static AncillaryDefinition DefinitionOf(ResolvedAncillaryDefinition definition, string selectionReference)
        {
            if (definition.DefinitionVersionId <= 0 || string.IsNullOrWhiteSpace(definition.ServiceDefinitionRef))
                throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionReference, nameof(ResolvedAncillaryDefinition));

            return new AncillaryDefinition(
                definition.DefinitionVersionId,
                definition.ServiceDefinitionRef,
                definition.ServiceTypeCode,
                definition.ServiceSubCode,
                definition.Rfic,
                definition.Rfisc,
                definition.SsrCode);
        }

        private static AncillaryProvision ProvisionOf(ResolvedAncillaryProvision provision, string selectionReference)
        {
            if (provision.ProvisionId <= 0)
                throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionReference, nameof(ResolvedAncillaryProvision));

            return new AncillaryProvision(
                provision.ProvisionId,
                provision.PurchaseStage,
                provision.BookingMethod,
                provision.BookingRequired,
                provision.ConfirmationRequirement,
                provision.FulfillmentProviderKey,
                provision.DocumentRouting,
                provision.DocumentType,
                provision.DocumentRequired,
                provision.RequiresCheckAtReserve);
        }

        private static BaggageChoice BaggageChoiceOf(ResolvedAncillaryLine line, AncillaryVariant variant)
        {
            var baggage = line.Details.Baggage
                          ?? throw ExceptionFactory.AncillaryChoiceIsMissing(line.OfferItemId, variant.Code, nameof(ResolvedAncillaryDetails.Baggage));

            if (variant.Code == AncillaryVariant.Overweight)
            {
                Require(line, variant, !string.IsNullOrWhiteSpace(baggage.BagRef), nameof(baggage.BagRef));
                Require(line, variant, baggage.MeasuredWeightKg is not null, nameof(baggage.MeasuredWeightKg));
            }

            if (variant.Code == AncillaryVariant.Oversize)
                Require(line, variant, baggage.Dimensions is not null, nameof(baggage.Dimensions));

            if (variant.Code == AncillaryVariant.SpecialEquipment)
                Require(line, variant, !string.IsNullOrWhiteSpace(baggage.EquipmentKind), nameof(baggage.EquipmentKind));

            Valid(line, variant, baggage.PackageWeightKg is null or > 0, nameof(baggage.PackageWeightKg));
            Valid(line, variant, baggage.MeasuredWeightKg is null or > 0, nameof(baggage.MeasuredWeightKg));
            Valid(line, variant, baggage.WeightKg is null or > 0, nameof(baggage.WeightKg));

            return new BaggageChoice(
                baggage.ChargeKind,
                baggage.AllowanceConcept,
                baggage.PackageWeightKg,
                baggage.WeightFromExclusiveKg,
                baggage.WeightToInclusiveKg,
                baggage.EquipmentKind,
                baggage.BagRef,
                baggage.MeasuredWeightKg,
                baggage.Dimensions is null ? null : DimensionsOf(line, variant, baggage.Dimensions, nameof(baggage.Dimensions)),
                baggage.WeightKg);
        }

        private static MealChoice MealChoiceOf(ResolvedAncillaryLine line, AncillaryVariant variant, ResolvedMealDetails meal)
        {
            if (variant.Code == AncillaryVariant.FreeSpecialMeal)
                Require(line, variant, !string.IsNullOrWhiteSpace(meal.MealCode), nameof(meal.MealCode));

            if (variant.Code == AncillaryVariant.PaidPreorderMeal)
                Require(line, variant, !string.IsNullOrWhiteSpace(meal.MenuItemRef), nameof(meal.MenuItemRef));

            return new MealChoice(meal.MealKind, meal.MealCode, meal.MenuItemRef, meal.DietaryCode, meal.ExclusiveMealFamilyCode);
        }

        private static PetChoice PetChoiceOf(ResolvedAncillaryLine line, AncillaryVariant variant, ResolvedPetDetails pet)
        {
            var expectedMode = variant.Code == AncillaryVariant.PetInHold ? PetTransportMode.Hold : PetTransportMode.Cabin;

            Valid(line, variant, pet.TransportMode == expectedMode, nameof(pet.TransportMode));
            Require(line, variant, pet.AnimalType is not null, nameof(pet.AnimalType));
            Require(line, variant, pet.CombinedWeightKg is not null, nameof(pet.CombinedWeightKg));
            Require(line, variant, pet.CarrierDimensions is not null, nameof(pet.CarrierDimensions));
            Valid(line, variant, pet.CombinedWeightKg > 0, nameof(pet.CombinedWeightKg));
            Valid(line, variant, pet.AnimalAgeWeeks is null or >= 0, nameof(pet.AnimalAgeWeeks));

            return new PetChoice(
                pet.TransportMode,
                pet.AnimalType!.Value,
                pet.OtherAnimalCode,
                pet.AnimalAgeWeeks,
                pet.CombinedWeightKg!.Value,
                DimensionsOf(line, variant, pet.CarrierDimensions!, nameof(pet.CarrierDimensions)),
                pet.DocumentAcknowledgements,
                pet.SizeBracket);
        }

        private AssistanceChoice AssistanceChoiceOf(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            ResolvedAssistanceDetails assistance,
            ShoppingSourceKind sourceKind)
        {
            var expectedKind = variant.Code switch
            {
                AncillaryVariant.Wheelchair => AssistanceKind.Wheelchair,
                AncillaryVariant.DisabilityAssistance => AssistanceKind.DisabilityAssistance,
                AncillaryVariant.MedicalEquipment => AssistanceKind.MedicalEquipment,
                AncillaryVariant.Bassinet => AssistanceKind.Bassinet,
                _ => AssistanceKind.UnaccompaniedMinor
            };

            Valid(line, variant, assistance.AssistanceKind == expectedKind, nameof(assistance.AssistanceKind));

            if (variant.Code is AncillaryVariant.Wheelchair or AncillaryVariant.DisabilityAssistance)
                Require(line, variant, !string.IsNullOrWhiteSpace(assistance.AssistanceSsrCode), nameof(assistance.AssistanceSsrCode));

            if (variant.Code == AncillaryVariant.Bassinet)
            {
                Require(line, variant, assistance.InfantRef is not null, nameof(assistance.InfantRef));
                Require(line, variant, assistance.GuardianRef is not null, nameof(assistance.GuardianRef));
            }

            return new AssistanceChoice(
                assistance.AssistanceKind,
                assistance.AssistanceSsrCode,
                assistance.InfantRef is null ? null : TravellerOf(line.OfferItemId, assistance.InfantRef, sourceKind).Id,
                assistance.GuardianRef is null ? null : TravellerOf(line.OfferItemId, assistance.GuardianRef, sourceKind).Id);
        }

        private static MedicalEquipmentChoice MedicalEquipmentChoiceOf(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            ResolvedMedicalEquipmentDetails medicalEquipment)
        {
            Require(line, variant, !string.IsNullOrWhiteSpace(medicalEquipment.EquipmentCode), nameof(medicalEquipment.EquipmentCode));
            Valid(line, variant, medicalEquipment.OxygenUnits is null or > 0, nameof(medicalEquipment.OxygenUnits));
            Valid(line, variant, medicalEquipment.OxygenFlowRateLitersPerMinute is null or > 0, nameof(medicalEquipment.OxygenFlowRateLitersPerMinute));

            return new MedicalEquipmentChoice(
                medicalEquipment.EquipmentCode!,
                medicalEquipment.EvidenceDocumentRefs,
                medicalEquipment.OxygenUnits,
                medicalEquipment.OxygenFlowRateLitersPerMinute,
                medicalEquipment.FrequentTravellerMedicalCardNumber);
        }

        private static UnaccompaniedMinorChoice UnaccompaniedMinorChoiceOf(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            ResolvedUnaccompaniedMinorDetails unaccompaniedMinor)
        {
            Require(line, variant, !string.IsNullOrWhiteSpace(unaccompaniedMinor.GuardianHandoffContact), nameof(unaccompaniedMinor.GuardianHandoffContact));
            Require(line, variant, !string.IsNullOrWhiteSpace(unaccompaniedMinor.GuardianPickupContact), nameof(unaccompaniedMinor.GuardianPickupContact));

            return new UnaccompaniedMinorChoice(unaccompaniedMinor.GuardianHandoffContact!, unaccompaniedMinor.GuardianPickupContact!);
        }

        private static AirportServiceChoice AirportServiceChoiceOf(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            ResolvedAirportServiceDetails airportService)
        {
            Valid(line, variant, airportService.AirportId > 0, nameof(airportService.AirportId));
            Valid(line, variant, airportService.GuestCount is null or >= 0, nameof(airportService.GuestCount));

            return new AirportServiceChoice(
                airportService.Kind,
                airportService.AirportId,
                airportService.TerminalRef,
                airportService.FacilityId,
                airportService.Direction,
                airportService.TimeWithOffset,
                airportService.GuestCount);
        }

        private static PriorityChoice PriorityChoiceOf(ResolvedAncillaryLine line, AncillaryVariant variant, ResolvedPriorityDetails priority)
        {
            Require(line, variant, priority.OptIn is not null, nameof(priority.OptIn));
            Valid(line, variant, priority.OptIn == true, nameof(priority.OptIn));

            return new PriorityChoice(priority.Kind, priority.PriorityZoneCode, priority.PriorityGroupCode, priority.FareBenefitRef, priority.OptIn);
        }

        private static ConnectivityChoice ConnectivityChoiceOf(
            ResolvedAncillaryLine line,
            AncillaryVariant variant,
            ResolvedConnectivityDetails connectivity)
        {
            Valid(line, variant, connectivity.DeviceCount is null or > 0, nameof(connectivity.DeviceCount));

            return new ConnectivityChoice(
                connectivity.PlanKind,
                connectivity.DurationMinutes,
                connectivity.IncludedDataMb,
                connectivity.MaxDevices,
                connectivity.DeliveryStage,
                connectivity.DeviceCount);
        }

        private static UpgradeChoice UpgradeChoiceOf(ResolvedAncillaryLine line, ResolvedUpgradeDetails upgrade)
        {
            if (upgrade.AllowedUpgradeKind != UpgradeKind.FixedAncillary)
                throw ExceptionFactory.AncillaryUpgradeIsNotFixedPriced(line.OfferItemId, upgrade.AllowedUpgradeKind);

            return new UpgradeChoice(upgrade.FromCabinId, upgrade.ToCabinId, upgrade.AllowedUpgradeKind, upgrade.RequiresTicketExchange);
        }

        private static ExtraSeatChoice ExtraSeatChoiceOf(ResolvedAncillaryLine line, AncillaryVariant variant, ResolvedExtraSeatDetails extraSeat)
        {
            Require(line, variant, !string.IsNullOrWhiteSpace(extraSeat.SeatNumber), nameof(extraSeat.SeatNumber));
            Valid(line, variant, extraSeat.ExtraOccupiedSeatCount is null or > 0, nameof(extraSeat.ExtraOccupiedSeatCount));

            return new ExtraSeatChoice(extraSeat.SeatPurpose, extraSeat.ExtraSeatPurpose, extraSeat.ExtraOccupiedSeatCount, extraSeat.SeatNumber!);
        }

        private static ServiceDimensions DimensionsOf(ResolvedAncillaryLine line, AncillaryVariant variant, ResolvedDimensions dimensions, string choice)
        {
            Valid(line, variant, dimensions is { LengthCm: > 0, WidthCm: > 0, HeightCm: > 0 }, choice);

            return new ServiceDimensions(dimensions.LengthCm, dimensions.WidthCm, dimensions.HeightCm);
        }

        private static void Require(ResolvedAncillaryLine line, AncillaryVariant variant, bool isPresent, string choice)
        {
            if (!isPresent)
                throw ExceptionFactory.AncillaryChoiceIsMissing(line.OfferItemId, variant.Code, choice);
        }

        private static void Valid(ResolvedAncillaryLine line, AncillaryVariant variant, bool isValid, string choice)
        {
            if (!isValid)
                throw ExceptionFactory.AncillaryChoiceIsInvalid(line.OfferItemId, variant.Code, choice);
        }

        private sealed record PlannedPurchase(OrderItem Item, OrderService Service, IReadOnlyList<PricingLine> PricingLines);
    }
}
