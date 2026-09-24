using System.Globalization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain.Providers.Pricing;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers._Shared;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Providers.FlightFlow.Services
{
    public sealed class FlightFlowReservationProvider : IReservationProvider
    {
        private const decimal HoldRevenue = 0m;

        private static readonly ReservationCapability Capability = new(
            ReservationMode.HoldThenConfirm,
            BatchResultMode.AtomicAllOrNothing,
            PreConfirmationReleaseScope: ReservationActionScope.Operation,
            PostConfirmationCancelScope: ReservationActionScope.Unit,
            SupportsExtend: true,
            SupportsSplit: true,
            ProvidesUnitReference: true);

        private readonly IFlightFlowProvider _flightFlow;
        private readonly IAirFareReservationValidator _airFareValidator;
        private readonly IClock _clock;
        private readonly TimeSpan _holdDuration;

        public FlightFlowReservationProvider(
            IFlightFlowProvider flightFlow,
            IAirFareReservationValidator airFareValidator,
            IClock clock,
            IOptions<FlightFlowReservationOptions> options)
        {
            _flightFlow = flightFlow;
            _airFareValidator = airFareValidator;
            _clock = clock;
            _holdDuration = TimeSpan.FromMinutes(options.Value.HoldMinutes);
        }

        public string ProviderKey => FulfillmentProviderKeys.FlightFlow;

        public ReservationCapability CapabilityFor(OrderService service) => Capability;

        public IReadOnlyList<ReservationUnitIntent> PlanUnits(Order order, IReadOnlyCollection<OrderService> services)
        {
            var index = new FlightUnitIndex(order, ProviderKey);

            return services
                .Select(index.SeatConsumingAirServiceOf)
                .DistinctBy(airService => airService.Id)
                .Select(index.UnitFor)
                .ToList();
        }

        public async Task<ReservationPreparation> PrepareAsync(
            Order order,
            IReadOnlyList<ReservationUnitIntent> units,
            CancellationToken cancellationToken = default)
        {
            var index = new FlightUnitIndex(order, ProviderKey);
            var memberIds = units.SelectMany(unit => unit.OrderServiceIds).ToHashSet();

            foreach (var paxReference in units.Select(unit => DetailsOf(unit).PaxReference).Distinct())
                AirPricePassengerTypes.From(index.Traveller(paxReference).PassengerType, FulfillmentProviderKeys.FlightFlow);

            var timeLimit = await _airFareValidator.ValidateAsync(order, index.AirServiceIdsAmong(memberIds), cancellationToken);

            return new ReservationPreparation(Earliest(_clock.GetDateTime() + _holdDuration, order.LastTicketingDate, timeLimit));
        }

        public async Task<ReservationOutcome> ReserveAsync(
            Order order,
            ReservationIntent intent,
            CancellationToken cancellationToken = default)
        {
            var request = HoldRequestFor(new FlightUnitIndex(order, ProviderKey), intent);

            FlightHeldSeatsResult result;

            try
            {
                result = await _flightFlow.CreateHoldAsync(request, cancellationToken);
            }
            catch (ProviderRequestException exception)
            {
                return new ReservationOutcome(OutcomeOf(exception), null, null, null, null, [], FailureOf(exception));
            }

            return Normalize(intent, result);
        }

        public async Task<ReleaseOutcome> ReleaseAsync(ReleaseIntent intent, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _flightFlow.ReleaseHeldAsync(new ReleaseHeldSeatsRequest(intent.ProviderOperationRef), cancellationToken);

                return result.Released
                    ? new ReleaseOutcome(ProviderOperationOutcome.Succeeded, null)
                    : new ReleaseOutcome(
                        ProviderOperationOutcome.Rejected,
                        new ProviderFailure(FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, result.Reason ?? string.Empty, null));
            }
            catch (ProviderRequestException exception)
            {
                return new ReleaseOutcome(OutcomeOf(exception), FailureOf(exception));
            }
        }

        private static HoldSeatsRequest HoldRequestFor(FlightUnitIndex index, ReservationIntent intent)
        {
            var units = intent.Units.Select(DetailsOf).ToList();

            var passengers = units
                .Select(unit => unit.PaxReference)
                .Distinct()
                .Select(paxReference =>
                {
                    var traveller = index.Traveller(paxReference);
                    return new PassengerForHoldSeatRequest(
                        paxReference,
                        AirPricePassengerTypes.From(traveller.PassengerType, FulfillmentProviderKeys.FlightFlow),
                        index.GenderOf(traveller) ?? Gender.UnSpecified);
                })
                .ToList();

            var flights = units
                .GroupBy(unit => unit.FlightCapacityId)
                .Select(capacity => new FlightForHoldSeatRequest(
                    capacity.Key.ToString(CultureInfo.InvariantCulture),
                    capacity.Select(unit => new SeatForHoldSeatRequest(unit.PaxReference, HoldRevenue, unit.RequestedSeat)).ToList()))
                .ToList();

            return new HoldSeatsRequest(
                intent.IdempotencyKey,
                intent.CorrelationReference,
                intent.RequestedExpiresAt!.Value,
                passengers,
                flights);
        }

        private static ReservationOutcome Normalize(ReservationIntent intent, FlightHeldSeatsResult result)
        {
            if (InconsistencyOf(intent, result) is { } inconsistency)
                return new ReservationOutcome(
                    ProviderOperationOutcome.Unknown,
                    string.IsNullOrWhiteSpace(result.HoldId) ? null : result.HoldId,
                    result.IdempotencyKey,
                    result.Reference,
                    null,
                    [],
                    new ProviderFailure(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, inconsistency, null));

            var seatsByUnit = result.Seats.ToDictionary(seat => UnitKey(seat.FlightId, seat.PaxReference), StringComparer.Ordinal);

            var units = intent.Units
                .Select(unit =>
                {
                    var seat = seatsByUnit[unit.UnitCorrelationKey];
                    var status = seat.SeatHoldStatus!.Value;

                    return new ReservationUnitOutcome(
                        unit.UnitCorrelationKey,
                        unit.OrderServiceIds,
                        ToUnitStatus(status),
                        seat.SeatHoldReference,
                        status.ToString(),
                        seat.Seat);
                })
                .ToList();

            return new ReservationOutcome(
                ProviderOperationOutcome.Succeeded,
                result.HoldId,
                result.IdempotencyKey,
                result.Reference,
                result.ExpiresAt,
                units,
                null);
        }

        private static string? InconsistencyOf(ReservationIntent intent, FlightHeldSeatsResult result)
        {
            if (string.IsNullOrWhiteSpace(result.HoldId))
                return "The hold response carried no hold id.";

            if (!string.Equals(result.IdempotencyKey, intent.IdempotencyKey, StringComparison.Ordinal))
                return "The hold response echoed a different idempotency key.";

            if (!string.Equals(result.Reference, intent.CorrelationReference, StringComparison.Ordinal))
                return "The hold response echoed a different reference.";

            var received = result.Seats.Select(seat => UnitKey(seat.FlightId, seat.PaxReference)).ToList();

            if (received.Count != received.Distinct(StringComparer.Ordinal).Count())
                return "The hold response contained more than one seat for the same passenger and flight.";

            if (!intent.Units.Select(unit => unit.UnitCorrelationKey).ToHashSet(StringComparer.Ordinal).SetEquals(received))
                return "The hold response did not cover exactly the requested passenger-flight units.";

            if (result.Seats.Any(seat => seat.SeatHoldStatus is null || string.IsNullOrWhiteSpace(seat.SeatHoldReference)))
                return "The hold response omitted a seat hold status or reference.";

            return null;
        }

        private static ReservationMemberStatus ToUnitStatus(FlightSeatHoldStatus status) => status switch
        {
            FlightSeatHoldStatus.Held => ReservationMemberStatus.Held,
            FlightSeatHoldStatus.Confirmed => ReservationMemberStatus.Confirmed,
            FlightSeatHoldStatus.Released => ReservationMemberStatus.Released,
            FlightSeatHoldStatus.Expired => ReservationMemberStatus.Expired,
            FlightSeatHoldStatus.Cancelled => ReservationMemberStatus.Cancelled,
            _ => ReservationMemberStatus.Unknown
        };

        private static ProviderOperationOutcome OutcomeOf(ProviderRequestException exception)
            => exception.Kind == FulfillmentFailureKind.Permanent
                ? ProviderOperationOutcome.Rejected
                : ProviderOperationOutcome.Unknown;

        private static ProviderFailure FailureOf(ProviderRequestException exception)
            => new(exception.Kind, exception.Reason, exception.Message, exception.StatusCode);

        private static FlightReservationUnitDetails DetailsOf(ReservationUnitIntent unit)
            => (FlightReservationUnitDetails)unit.Details;

        private static DateTimeOffset Earliest(DateTimeOffset holdLimit, DateTimeOffset? lastTicketingDate, DateTimeOffset timeLimit)
            => new[] { holdLimit, lastTicketingDate ?? holdLimit, timeLimit }.Min();

        private static string UnitKey(string flightId, string paxReference) => $"{flightId}:{paxReference}";

        private sealed class FlightUnitIndex
        {
            private readonly Dictionary<long, OrderTraveller> _travellers;
            private readonly Dictionary<long, OrderSegment> _segments;
            private readonly Dictionary<long, OrderAirTransportService> _airServices;
            private readonly List<OrderSeatService> _seatServices;

            public FlightUnitIndex(Order order, string providerKey)
            {
                _travellers = order.Travellers.ToDictionary(traveller => traveller.Id);
                _segments = order.Segments.ToDictionary(segment => segment.Id);

                var services = order.Services
                    .Where(service => service.FulfillmentProviderKey == providerKey
                                      && service.CommercialStatus == OrderServiceCommercialState.Active)
                    .ToList();

                _airServices = services.OfType<OrderAirTransportService>().ToDictionary(service => service.Id);
                _seatServices = services.OfType<OrderSeatService>().ToList();
            }

            public OrderTraveller Traveller(string paxReference)
                => _travellers[long.Parse(paxReference, CultureInfo.InvariantCulture)];

            public Gender? GenderOf(OrderTraveller traveller)
                => traveller.ProfileRevisions.Single(revision => revision.Id == traveller.CurrentProfileRevisionId).Gender;

            public IReadOnlyCollection<long> AirServiceIdsAmong(IReadOnlySet<long> serviceIds)
                => serviceIds.Where(_airServices.ContainsKey).ToList();

            public OrderAirTransportService SeatConsumingAirServiceOf(OrderService service)
            {
                var airService = service switch
                {
                    OrderAirTransportService air => air,
                    OrderSeatService seat => _airServices[seat.AssociatedAirServiceId],
                    _ => throw ExceptionFactory.NoFulfillmentAdapterRegistered(FulfillmentProviderKeys.FlightFlow, service.ServiceType)
                };

                if (_travellers[airService.TravellerId].InfantParentTravellerId is not { } parentTravellerId)
                    return airService;

                return _airServices.Values.FirstOrDefault(parent => parent.TravellerId == parentTravellerId
                                                                    && parent.SegmentId == airService.SegmentId)
                       ?? throw ExceptionFactory.OrderHasNoAirServiceForFlight(_segments[airService.SegmentId].FlightId);
            }

            public ReservationUnitIntent UnitFor(OrderAirTransportService owner)
            {
                var segment = _segments[owner.SegmentId];

                var infantAirServiceIds = _airServices.Values
                    .Where(service => service.SegmentId == owner.SegmentId
                                      && _travellers[service.TravellerId].InfantParentTravellerId == owner.TravellerId)
                    .Select(service => service.Id)
                    .ToList();

                var coveredAirServiceIds = infantAirServiceIds.Prepend(owner.Id).ToList();

                var seats = _seatServices
                    .Where(seat => coveredAirServiceIds.Contains(seat.AssociatedAirServiceId))
                    .ToList();

                var paxReference = owner.TravellerId.ToString(CultureInfo.InvariantCulture);

                return new ReservationUnitIntent(
                    UnitKey(segment.FlightId.ToString(CultureInfo.InvariantCulture), paxReference),
                    coveredAirServiceIds.Concat(seats.Select(seat => seat.Id)).ToList(),
                    new FlightReservationUnitDetails(
                        segment.FlightId,
                        owner.FlightCapacityId,
                        paxReference,
                        seats.FirstOrDefault(seat => seat.AssociatedAirServiceId == owner.Id)?.SeatNumber));
            }
        }
    }
}
