using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.DomainEvents;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        public static Order Create(CreateOrderArgs args, OfferDetail offer, IIdGenerator idGenerator, IClock clock)
        {
            var reader = new OfferReader(offer);
            ReconcileTravellers(args, reader);

            var createdAt = clock.GetDateTime();
            var order = new Order(
                idGenerator.NewId(),
                Guid.NewGuid(),
                args.CustomerId,
                args.SalesContext,
                reader.CurrencyId,
                reader.OfferId,
                reader.LastTicketingDate,
                createdAt);

            var change = order.RecordCreateChange(args, reader, idGenerator, createdAt);

            order.BuildContact(args, idGenerator);
            order.BuildTravellers(args, reader, idGenerator, change.Id, createdAt);
            order.BuildItinerary(reader, idGenerator);
            order.BuildItemsAndServices(args, reader, idGenerator, change.Id, createdAt);
            order.BuildFarePricingUnits(reader, idGenerator, change.Id);
            order.BuildPricing(reader, idGenerator, change.Id, createdAt);
            order.BuildRemarks(args, idGenerator, createdAt);
            order.ReconcileTotals();
            order.RaiseCreated(idGenerator, createdAt);

            return order;
        }

        private static void ReconcileTravellers(CreateOrderArgs args, OfferReader reader)
        {
            var requested = args.Travellers.Select(traveller => traveller.Index).ToList();
            var offered = reader.Travellers.Select(traveller => traveller.TravellerIndex).ToList();

            if (requested.Count != requested.Distinct().Count())
                throw ExceptionFactory.TravellerIndexIsDuplicated();

            if (offered.Count != offered.Distinct().Count())
                throw ExceptionFactory.OfferTravellerIndexIsDuplicated();

            if (!requested.OrderBy(index => index).SequenceEqual(offered.OrderBy(index => index)))
                throw ExceptionFactory.TravellerSetDoesNotMatchOffer();

            foreach (var traveller in args.Travellers)
            {
                var offeredCode = reader.Traveller(traveller.Index).PassengerTypeCode;

                if (!Enum.TryParse<PassengerTypeCode>(offeredCode, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                    throw ExceptionFactory.OfferPassengerCodeIsNotRecognised(offeredCode);

                if (parsed != traveller.PassengerType)
                    throw ExceptionFactory.TravellerPassengerTypeDoesNotMatchOffer(traveller.Index);
            }

            if (args.Travellers.All(traveller => traveller.AgeRange != AgeRange.Adult))
                throw ExceptionFactory.OrderMustIncludeAtLeastOneAdult();

            if (args.Travellers.Count(traveller => traveller.AgeRange == AgeRange.Infant)
                > args.Travellers.Count(traveller => traveller.AgeRange == AgeRange.Adult))
                throw ExceptionFactory.OrderCannotHaveMoreInfantsThanAdults();

            foreach (var infant in args.Travellers.Where(traveller => traveller.AgeRange == AgeRange.Infant))
            {
                if (infant.InfantParentIndex is not { } parentIndex)
                    throw ExceptionFactory.InfantRequiresParentTraveller(infant.Index);

                var parent = args.Travellers.FirstOrDefault(traveller => traveller.Index == parentIndex);
                if (parent is null || parent.AgeRange != AgeRange.Adult)
                    throw ExceptionFactory.InfantParentMustBeAnAdult(infant.Index);
            }
        }

        private OrderChange RecordCreateChange(
            CreateOrderArgs args,
            OfferReader reader,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt)
        {
            var change = new OrderChange(
                idGenerator.NewId(),
                Id,
                OrderChangeType.Create,
                CommercialVersion,
                args.SalesContext.Copy(),
                reader.OfferId,
                createdAt);

            _changes.Add(change);
            return change;
        }

        private void BuildContact(CreateOrderArgs args, IIdGenerator idGenerator)
        {
            var contact = new OrderContact(idGenerator.NewId(), Id, 1, ContactRole.Primary, args.Contact.ContactName);

            foreach (var point in args.Contact.ContactPoints)
                contact.AddContactPoint(new OrderContactPoint(
                    idGenerator.NewId(),
                    contact.Id,
                    point.Type,
                    point.Value,
                    point.CountryCode,
                    point.IsPrimary));

            _contacts.Add(contact);
        }

        private void BuildTravellers(
            CreateOrderArgs args,
            OfferReader reader,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt)
        {
            foreach (var source in args.Travellers.OrderBy(traveller => traveller.Index))
            {
                var traveller = new OrderTraveller(
                    idGenerator.NewId(),
                    Id,
                    source.Index,
                    reader.Traveller(source.Index).TravellerRef,
                    source.PassengerType,
                    source.AgeRange);

                traveller.AddProfileRevision(new TravellerProfileRevision(
                    idGenerator.NewId(),
                    traveller.Id,
                    source.GivenName,
                    source.Surname,
                    source.NoSurname,
                    source.DateOfBirth,
                    source.Gender,
                    source.NationalityId,
                    source.CountryOfResidenceId,
                    changeId,
                    createdAt));

                foreach (var document in source.Documents)
                    traveller.AddDocument(new OrderTravellerDocument(
                        idGenerator.NewId(),
                        traveller.Id,
                        document.Type,
                        document.Number,
                        document.ExpiryDate,
                        document.IssuanceCountryId,
                        document.Holder));

                _travellers.Add(traveller);
            }

            foreach (var infant in args.Travellers.Where(traveller => traveller.InfantParentIndex.HasValue))
            {
                var parent = TravellerByIndex(infant.InfantParentIndex!.Value);
                TravellerByIndex(infant.Index).AssignInfantParent(parent.Id);
            }
        }

        private void BuildItinerary(OfferReader reader, IIdGenerator idGenerator)
        {
            foreach (var bound in reader.BoundsInSequence())
            {
                var journey = new OrderJourney(
                    idGenerator.NewId(),
                    Id,
                    bound.Sequence,
                    bound.BoundId,
                    (int)bound.OriginAirportId,
                    (int)bound.DestinationAirportId);

                _journeys.Add(journey);

                foreach (var flight in reader.BoundFlightsInOrder(bound))
                {
                    var segment = new OrderSegment(
                        idGenerator.NewId(),
                        Id,
                        journey.Id,
                        flight.Sequence,
                        flight.FlightId,
                        flight.FlightVersion,
                        flight.FlightNumber,
                        (int)flight.OriginAirportId,
                        (int?)flight.OriginAirportTerminalId,
                        (int)flight.DestinationAirportId,
                        (int?)flight.DestinationAirportTerminalId,
                        (int)flight.OperatingAirlineId,
                        (int)flight.MarketingAirlineId,
                        flight.DepartureDateTime,
                        flight.ArrivalDateTime,
                        flight.Duration,
                        (int?)flight.AircraftId);

                    foreach (var leg in flight.Legs.OrderBy(item => item.Sequence))
                        segment.AddLeg(new OrderSegmentLeg(
                            idGenerator.NewId(),
                            segment.Id,
                            leg.Sequence,
                            leg.LegId,
                            (int)leg.OriginAirportId,
                            (int?)leg.OriginAirportTerminalId,
                            (int)leg.DestinationAirportId,
                            (int?)leg.DestinationAirportTerminalId,
                            leg.DepartureDateTime,
                            leg.ArrivalDateTime,
                            leg.Stop?.StopType,
                            leg.Stop?.DurationMinutes,
                            leg.Stop?.PassengersCanBoardOrLeave));

                    _segments.Add(segment);
                }
            }
        }

        private void BuildItemsAndServices(
            CreateOrderArgs args,
            OfferReader reader,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt)
        {
            foreach (var traveller in _travellers.OrderBy(traveller => traveller.Index))
            {
                foreach (var bound in reader.BoundsInSequence())
                {
                    var item = new OrderItem(idGenerator.NewId(), Id, ProductType.AirFare, changeId, createdAt);
                    _items.Add(item);

                    foreach (var flight in reader.BoundFlightsInOrder(bound))
                    {
                        var airFareId = reader.ResolveCouponAirFareId(traveller.SourceTravellerRef!, bound.BoundId, flight.FlightId);
                        var fareComponent = reader.FareComponent(bound.BoundId, airFareId);
                        var coupon = reader.Coupon(traveller.SourceTravellerRef!, flight.FlightId);
                        var segment = SegmentOf(flight.FlightId);

                        _services.Add(new OrderAirTransportService(
                            idGenerator.NewId(),
                            Id,
                            item.Id,
                            traveller.Id,
                            segment.Id,
                            flight.FlightCapacityId,
                            airFareId,
                            fareComponent?.BookingClass,
                            fareComponent?.FareBasis,
                            fareComponent?.FareFamily,
                            fareComponent?.FareType,
                            flight.RbdId,
                            ToAllowance(coupon.CheckedBaggage),
                            ToAllowance(coupon.CabinBaggage),
                            coupon.IsRefundable,
                            coupon.IsChangeable,
                            coupon.IsUpgradable,
                            FulfillmentProviderKeys.FlightFlow,
                            changeId,
                            createdAt));
                    }
                }
            }

            BuildSeatServices(args, idGenerator, changeId, createdAt);
        }

        private void BuildFarePricingUnits(OfferReader reader, IIdGenerator idGenerator, long changeId)
        {
            var journeyIdByBound = _journeys.ToDictionary(journey => journey.BoundId, journey => journey.Id, StringComparer.OrdinalIgnoreCase);
            var journeySequenceById = _journeys.ToDictionary(journey => journey.Id, journey => journey.Sequence);
            var travellerIdByRef = _travellers.ToDictionary(traveller => traveller.SourceTravellerRef!, traveller => traveller.Id, StringComparer.OrdinalIgnoreCase);
            var segmentIdByFlight = _segments.ToDictionary(segment => segment.FlightId, segment => segment.Id);
            var airServiceByTravellerSegment = _services.OfType<OrderAirTransportService>().ToDictionary(service => (service.TravellerId, service.SegmentId));
            var coveredServiceIds = new HashSet<long>();

            foreach (var source in reader.PricingUnits)
            {
                var sourceJourneyIds = source.CoveredBoundIds
                    .Select(boundId => journeyIdByBound.TryGetValue(boundId, out var journeyId)
                        ? journeyId
                        : throw ExceptionFactory.FarePricingUnitBoundIsNotInOrder(source.Sequence, boundId))
                    .ToHashSet();

                var components = new List<(OfferFareComponent Source, List<OrderAirTransportService> Services)>();

                foreach (var component in source.FareComponents)
                {
                    if (!journeyIdByBound.TryGetValue(component.BoundId, out var componentJourneyId) || !sourceJourneyIds.Contains(componentJourneyId))
                        throw ExceptionFactory.FareComponentBoundIsNotInPricingUnit(source.Sequence, component.Sequence, component.BoundId);

                    var services = reader.CouponsPricedBy(component.AirFareId, component.BoundId)
                        .Select(coupon => airServiceByTravellerSegment[(travellerIdByRef[coupon.TravellerRef], segmentIdByFlight[coupon.FlightId])])
                        .ToList();

                    if (services.Count == 0)
                        throw ExceptionFactory.FareComponentCoversNoAirService(source.Sequence, component.Sequence);

                    foreach (var service in services)
                    {
                        if (!coveredServiceIds.Add(service.Id))
                            throw ExceptionFactory.FareComponentCoverageIsAmbiguous(source.Sequence, component.AirFareId, component.BoundId);

                        if (!Agrees(service, component))
                            throw ExceptionFactory.FareComponentContradictsAirService(source.Sequence, component.Sequence, service.Id);
                    }

                    components.Add((component, services));
                }

                var coveredJourneyIds = components
                    .SelectMany(component => component.Services)
                    .Select(service => SegmentJourneyId(service.SegmentId))
                    .Distinct()
                    .OrderBy(journeyId => journeySequenceById[journeyId])
                    .ToList();

                if (!sourceJourneyIds.SetEquals(coveredJourneyIds))
                    throw ExceptionFactory.FarePricingUnitContradictsOffer(source.Sequence);

                var pricingUnit = new OrderFarePricingUnit(idGenerator.NewId(), Id, changeId, source.Sequence, source.Kind, coveredJourneyIds);

                foreach (var (component, services) in components)
                    pricingUnit.AddFareComponent(new OrderFareComponent(
                        idGenerator.NewId(),
                        pricingUnit.Id,
                        component.Sequence,
                        component.AirFareId,
                        component.BookingClass,
                        component.FareBasis,
                        component.FareFamily,
                        component.FareType,
                        services.Select(service => service.Id).ToList()));

                _farePricingUnits.Add(pricingUnit);
            }
        }

        private static bool Agrees(OrderAirTransportService service, OfferFareComponent component)
            => service.AirFareId == component.AirFareId
               && service.BookingClass == component.BookingClass
               && service.FareBasis == component.FareBasis
               && service.FareFamily == component.FareFamily
               && service.FareType == component.FareType;

        private void BuildSeatServices(
            CreateOrderArgs args,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt)
        {
            foreach (var selection in args.SeatSelections.Where(seat => !string.IsNullOrWhiteSpace(seat.SeatNumber)))
            {
                var traveller = TravellerByIndex(selection.TravellerIndex);
                var journey = _journeys.FirstOrDefault(item => string.Equals(item.BoundId, selection.BoundId, StringComparison.OrdinalIgnoreCase))
                              ?? throw ExceptionFactory.SeatSelectionBoundIsNotInOrder(selection.BoundId);

                var boundSegmentIds = _segments
                    .Where(segment => segment.OrderJourneyId == journey.Id)
                    .Select(segment => segment.Id)
                    .ToHashSet();

                var airServices = _services
                    .OfType<OrderAirTransportService>()
                    .Where(service => service.TravellerId == traveller.Id && boundSegmentIds.Contains(service.SegmentId))
                    .ToList();

                if (airServices.Count == 0)
                    throw ExceptionFactory.SeatSelectionHasNoMatchingAirService(selection.TravellerIndex, selection.BoundId);

                foreach (var airService in airServices)
                    _services.Add(new OrderSeatService(
                        idGenerator.NewId(),
                        Id,
                        airService.OrderItemId,
                        traveller.Id,
                        airService.SegmentId,
                        airService.Id,
                        selection.SeatNumber,
                        airService.FulfillmentProviderKey,
                        changeId,
                        createdAt));
            }
        }

        private void BuildRemarks(CreateOrderArgs args, IIdGenerator idGenerator, DateTimeOffset createdAt)
        {
            foreach (var remark in args.Remarks)
                AddRemarkInternal(remark, idGenerator, createdAt, supersedesRemarkId: null);
        }

        private void RaiseCreated(IIdGenerator idGenerator, DateTimeOffset createdAt)
            => Causes(new OrderCreated(
                idGenerator.NewId().ToString(),
                Id.ToString(),
                createdAt,
                Id,
                OrderReference,
                SourceOfferId,
                CustomerId,
                SalesContext.Channel,
                SalesContext.ActorId,
                SalesContext.OfficeKind,
                SalesContext.OfficeId,
                Status,
                CurrencyId,
                CustomerTotal,
                CommercialVersion,
                _travellers.Count,
                LastTicketingDate,
                CreatedAt));

        private static BaggageAllowance? ToAllowance(OfferBaggage? baggage)
            => baggage is null ? null : BaggageAllowance.FromSource(baggage.Pieces, baggage.Weight, baggage.Unit);

        private OrderTraveller TravellerByIndex(int index)
            => _travellers.FirstOrDefault(traveller => traveller.Index == index)
               ?? throw ExceptionFactory.OrderHasNoTravellerWithIndex(index);

        private OrderSegment SegmentOf(long flightId)
            => _segments.FirstOrDefault(segment => segment.FlightId == flightId)
               ?? throw ExceptionFactory.OrderHasNoSegmentForFlight(flightId);
    }
}
