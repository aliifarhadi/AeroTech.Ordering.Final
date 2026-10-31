using System.Globalization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
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

            var changeId = idGenerator.NewId();

            AcceptSelections(
                selection,
                seats,
                ShoppingSourceKind.Order,
                Id.ToString(CultureInfo.InvariantCulture),
                changeId,
                OrderPricingReason.AdditionalSale,
                idGenerator,
                now);

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

            ReconcileTotals();

            return change;
        }

        public void EnsureReservationIsSupported()
            => EnsureNoActiveService(service => service is OrderAncillaryService);

        public void EnsureIssuanceIsSupported()
            => EnsureNoActiveService(service => service is OrderAncillaryService or OrderSeatService { IsPricedSelection: true });

        private void EnsureNoActiveService(Func<OrderService, bool> isUnsupported)
        {
            var unsupported = _services.FirstOrDefault(service => service.IsActive && isUnsupported(service));

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

        private void AcceptSelections(
            ResolvedAncillarySelection? selection,
            ResolvedSeatQuote? seats,
            ShoppingSourceKind sourceKind,
            string sourceReference,
            long changeId,
            OrderPricingReason reason,
            IIdGenerator idGenerator,
            DateTimeOffset acceptedAt)
        {
            if (selection is not null)
                EnsureSelectionIsCurrent(selection, sourceKind, sourceReference, acceptedAt);

            if (seats is not null)
                EnsureSeatQuoteIsCurrent(seats, sourceKind, sourceReference, acceptedAt);

            var purchases = new List<PlannedPurchase>();

            if (selection is not null)
                PlanAncillaries(selection, changeId, reason, idGenerator, acceptedAt, purchases);

            if (seats is not null)
                PlanSeats(seats, changeId, reason, idGenerator, acceptedAt, purchases);

            Commit(purchases);
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
            EnsureSourceIsCurrent(
                quote.SelectionId,
                quote.SourceKind,
                quote.SourceReference,
                quote.OrderCommercialVersion,
                quote.ExpiresAt,
                sourceKind,
                sourceReference,
                now);

            if (quote.Seats.Count == 0)
                throw ExceptionFactory.AncillarySelectionIsEmpty(quote.SelectionId);

            if (quote.SelectedLineCount != quote.Seats.Count)
                throw ExceptionFactory.AncillarySelectionLineCountMismatch(quote.SelectionId, quote.SelectedLineCount, quote.Seats.Count);

            if (quote.Seats.GroupBy(seat => seat.SeatSelectionId, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } duplicate)
                throw ExceptionFactory.AncillaryOfferItemIsDuplicated(quote.SelectionId, duplicate.Key);
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
            if (actualKind != sourceKind
                || !string.Equals(actualReference, sourceReference, StringComparison.Ordinal)
                || (sourceKind == ShoppingSourceKind.Offer && actualCommercialVersion is not null))
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
                    new AncillarySaleSource(quote.SourceKind, quote.SourceReference, quote.OrderCommercialVersion, quote.SelectionId, seat.SeatSelectionId),
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
                SelectionScope.PerTraveller => (traveller, JourneyAirServicesOf(line, traveller, sourceKind)),
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

        private IReadOnlyList<OrderAirTransportService> JourneyAirServicesOf(
            ResolvedAncillaryLine line,
            OrderTraveller traveller,
            ShoppingSourceKind sourceKind)
        {
            if (line.Unit.PortionRef is not null || line.Unit.FlightRef is not null || line.Coverage.PortionRefs.Count > 0)
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            return sourceKind == ShoppingSourceKind.Offer
                ? OfferItineraryAirServicesOf(line, traveller)
                : NamedFlightAirServicesOf(line, traveller);
        }

        private IReadOnlyList<OrderAirTransportService> OfferItineraryAirServicesOf(ResolvedAncillaryLine line, OrderTraveller traveller)
        {
            if (line.Coverage.FlightRefs.Count > 0)
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);

            var airServices = _journeys
                .OrderBy(journey => journey.Sequence)
                .SelectMany(journey => _segments.Where(segment => segment.OrderJourneyId == journey.Id).OrderBy(segment => segment.Sequence))
                .Select(segment => ActiveAirServiceOf(traveller.Id, segment.Id)
                                   ?? throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId))
                .ToList();

            return airServices.Count > 0
                ? airServices
                : throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(line.OfferItemId);
        }

        private IReadOnlyList<OrderAirTransportService> NamedFlightAirServicesOf(ResolvedAncillaryLine line, OrderTraveller traveller)
        {
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
            var args = new OrderAncillaryServiceArgs(
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
                DefinitionOf(line.Definition, source.SelectionId),
                ProvisionOf(line.Provision, source.SelectionId),
                new AncillaryPricing(line.Pricing.PricingId, line.Pricing.PricingRateId, line.Pricing.PricingVersion, line.Pricing.PricingUnit),
                changeId,
                createdAt);

            var details = line.Details;

            return variant.Profile switch
            {
                AncillaryProfile.Baggage => BaggageServiceOf(args, DetailsOf(line, details.Baggage)),
                AncillaryProfile.Meal => MealServiceOf(args, DetailsOf(line, details.Meal)),
                AncillaryProfile.Pet => PetServiceOf(args, DetailsOf(line, details.Pet)),
                AncillaryProfile.AssistedTravel => AssistedTravelServiceOf(args, line, DetailsOf(line, details.Assistance), sourceKind),
                AncillaryProfile.AirportService => AirportServiceOf(args, DetailsOf(line, details.AirportService)),
                AncillaryProfile.Priority => PriorityServiceOf(args, DetailsOf(line, details.Priority)),
                AncillaryProfile.Connectivity => ConnectivityServiceOf(args, DetailsOf(line, details.Connectivity)),
                AncillaryProfile.Upgrade => UpgradeServiceOf(args, DetailsOf(line, details.Upgrade)),
                AncillaryProfile.Seat => ExtraSeatServiceOf(args, DetailsOf(line, details.ExtraSeat)),
                _ => throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(line.OfferItemId, variant.Code, variant.Profile)
            };
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

        private static OrderBaggageService BaggageServiceOf(OrderAncillaryServiceArgs args, ResolvedBaggageDetails baggage)
            => new(
                args,
                baggage.ChargeKind,
                baggage.AllowanceConcept,
                baggage.PackageWeightKg,
                baggage.WeightFromExclusiveKg,
                baggage.WeightToInclusiveKg,
                baggage.EquipmentKind,
                baggage.BagRef,
                baggage.MeasuredWeightKg,
                DimensionsOf(baggage.Dimensions),
                baggage.WeightKg);

        private static OrderMealService MealServiceOf(OrderAncillaryServiceArgs args, ResolvedMealDetails meal)
            => new(args, meal.MealKind, meal.MealCode, meal.MenuItemRef, meal.DietaryCode, meal.ExclusiveMealFamilyCode);

        private static OrderPetService PetServiceOf(OrderAncillaryServiceArgs args, ResolvedPetDetails pet)
            => new(
                args,
                pet.TransportMode,
                pet.AnimalType,
                pet.OtherAnimalCode,
                pet.AnimalAgeWeeks,
                pet.CombinedWeightKg,
                DimensionsOf(pet.CarrierDimensions),
                pet.DocumentAcknowledgements,
                pet.SizeBracket);

        private OrderAssistedTravelService AssistedTravelServiceOf(
            OrderAncillaryServiceArgs args,
            ResolvedAncillaryLine line,
            ResolvedAssistanceDetails assistance,
            ShoppingSourceKind sourceKind)
            => new(
                args,
                assistance.AssistanceKind,
                assistance.AssistanceSsrCode,
                assistance.InfantRef is null ? null : TravellerOf(line.OfferItemId, assistance.InfantRef, sourceKind).Id,
                assistance.GuardianRef is null ? null : TravellerOf(line.OfferItemId, assistance.GuardianRef, sourceKind).Id,
                assistance.MedicalEquipment is { } medical
                    ? new MedicalEquipmentChoice(
                        RequiredText(line, medical.EquipmentCode, nameof(medical.EquipmentCode)),
                        medical.EvidenceDocumentRefs,
                        medical.OxygenUnits,
                        medical.OxygenFlowRateLitersPerMinute,
                        medical.FrequentTravellerMedicalCardNumber)
                    : null,
                assistance.UnaccompaniedMinor is { } minor
                    ? new UnaccompaniedMinorChoice(
                        RequiredText(line, minor.GuardianHandoffContact, nameof(minor.GuardianHandoffContact)),
                        RequiredText(line, minor.GuardianPickupContact, nameof(minor.GuardianPickupContact)))
                    : null);

        private static OrderAirportService AirportServiceOf(OrderAncillaryServiceArgs args, ResolvedAirportServiceDetails airportService)
            => new(
                args,
                airportService.Kind,
                airportService.AirportId,
                airportService.TerminalRef,
                airportService.FacilityId,
                airportService.Direction,
                airportService.TimeWithOffset,
                airportService.GuestCount);

        private static OrderPriorityService PriorityServiceOf(OrderAncillaryServiceArgs args, ResolvedPriorityDetails priority)
            => new(args, priority.Kind, priority.PriorityZoneCode, priority.PriorityGroupCode, priority.FareBenefitRef, priority.OptIn);

        private static OrderConnectivityService ConnectivityServiceOf(OrderAncillaryServiceArgs args, ResolvedConnectivityDetails connectivity)
            => new(
                args,
                connectivity.PlanKind,
                connectivity.DurationMinutes,
                connectivity.IncludedDataMb,
                connectivity.MaxDevices,
                connectivity.DeliveryStage,
                connectivity.DeviceCount);

        private static OrderUpgradeService UpgradeServiceOf(OrderAncillaryServiceArgs args, ResolvedUpgradeDetails upgrade)
            => new(args, upgrade.FromCabinId, upgrade.ToCabinId, upgrade.AllowedUpgradeKind, upgrade.RequiresTicketExchange);

        private static OrderExtraSeatService ExtraSeatServiceOf(OrderAncillaryServiceArgs args, ResolvedExtraSeatDetails extraSeat)
            => new(args, extraSeat.SeatPurpose, extraSeat.ExtraSeatPurpose, extraSeat.ExtraOccupiedSeatCount, extraSeat.SeatNumber);

        private static TDetails DetailsOf<TDetails>(ResolvedAncillaryLine line, TDetails? details)
            where TDetails : class
            => details ?? throw ExceptionFactory.AncillaryChoiceIsMissing(line.OfferItemId, line.VariantCode, line.Profile);

        private static ServiceDimensions? DimensionsOf(ResolvedDimensions? dimensions)
            => dimensions is null ? null : new ServiceDimensions(dimensions.LengthCm, dimensions.WidthCm, dimensions.HeightCm);

        private static string RequiredText(ResolvedAncillaryLine line, string? value, string choice)
            => string.IsNullOrWhiteSpace(value)
                ? throw ExceptionFactory.AncillaryChoiceIsMissing(line.OfferItemId, line.VariantCode, choice)
                : value;

        private sealed record PlannedPurchase(OrderItem Item, OrderService Service, IReadOnlyList<PricingLine> PricingLines);
    }
}
