using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate
{
    public sealed class FulfillmentReservation : AggregateRoot<long>
    {
        private readonly List<ReservationUnit> _units = new();

        private FulfillmentReservation()
        {
        }

        private FulfillmentReservation(
            long id,
            long orderId,
            string fulfillmentProviderKey,
            ReservationMode mode,
            DateTimeOffset? requestedExpiresAt,
            DateTimeOffset? reservationValidationTimeLimit,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            FulfillmentProviderKey = fulfillmentProviderKey;
            Mode = mode;
            IdempotencyKey = $"reserve-hold:{id}";
            CorrelationReference = $"order:{orderId}:reservation:{id}";
            RequestedExpiresAt = requestedExpiresAt;
            ReservationValidationTimeLimit = reservationValidationTimeLimit;
            Status = FulfillmentReservationStatus.Pending;
            CreatedAt = createdAt;
            LastObservedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public string FulfillmentProviderKey { get; private set; } = default!;

        public ReservationMode Mode { get; private set; }

        public string IdempotencyKey { get; private set; } = default!;

        public string CorrelationReference { get; private set; } = default!;

        public string? ProviderOperationRef { get; private set; }

        public FulfillmentReservationStatus Status { get; private set; }

        public DateTimeOffset? RequestedExpiresAt { get; private set; }

        public DateTimeOffset? ReservationValidationTimeLimit { get; private set; }

        public DateTimeOffset? ExpiresAt { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset LastObservedAt { get; private set; }

        public IReadOnlyCollection<ReservationUnit> Units => _units.AsReadOnly();

        public bool IsUnresolved => Status is FulfillmentReservationStatus.Pending or FulfillmentReservationStatus.Unknown;

        public bool IsSettled => Status is FulfillmentReservationStatus.Rejected
            or FulfillmentReservationStatus.Released
            or FulfillmentReservationStatus.Expired
            or FulfillmentReservationStatus.Cancelled;

        public IReadOnlyCollection<long> CoveredOrderServiceIds => _units.SelectMany(unit => unit.OrderServiceIds).ToList();

        public static FulfillmentReservation Create(
            long id,
            long orderId,
            string fulfillmentProviderKey,
            ReservationMode mode,
            DateTimeOffset? requestedExpiresAt,
            DateTimeOffset? reservationValidationTimeLimit,
            IReadOnlyList<ReservationUnitIntent> units,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt)
        {
            if (units.Count == 0)
                throw ExceptionFactory.ReservationRequiresUnits();

            var duplicate = units
                .SelectMany(unit => unit.OrderServiceIds)
                .GroupBy(serviceId => serviceId)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicate is not null)
                throw ExceptionFactory.ReservationUnitCoversDuplicateService(duplicate.Key);

            var reservation = new FulfillmentReservation(
                id,
                orderId,
                fulfillmentProviderKey,
                mode,
                requestedExpiresAt,
                reservationValidationTimeLimit,
                createdAt);

            foreach (var unit in units)
                reservation._units.Add(new ReservationUnit(idGenerator.NewId(), id, unit.UnitCorrelationKey, unit.OrderServiceIds));

            return reservation;
        }

        public void EnsurePlannedAs(IReadOnlyList<ReservationUnitIntent> units)
        {
            var persisted = _units.ToDictionary(unit => unit.UnitCorrelationKey, unit => unit.OrderServiceIds, StringComparer.Ordinal);

            var matches = units.Count == persisted.Count
                          && units.All(unit => persisted.TryGetValue(unit.UnitCorrelationKey, out var serviceIds)
                                               && serviceIds.ToHashSet().SetEquals(unit.OrderServiceIds));

            if (!matches)
                throw ExceptionFactory.ReservationPlanDiffersFromPersistedUnits(Id);
        }

        public void RecordOutcome(ReservationOutcome outcome, DateTimeOffset observedAt)
        {
            if (!IsUnresolved)
                throw ExceptionFactory.ReservationOutcomeCannotBeRecorded(Id, Status);

            switch (outcome.OperationOutcome)
            {
                case ProviderOperationOutcome.Succeeded:
                case ProviderOperationOutcome.Partial:
                    AssignProviderOperationRef(outcome.ProviderOperationRef);
                    ExpiresAt = outcome.ExpiresAt;
                    ApplyUnitOutcomes(outcome.Units);
                    Status = StatusFromUnits();
                    break;

                case ProviderOperationOutcome.Rejected:
                    MarkAll(FulfillmentReservationStatus.Rejected, ReservationMemberStatus.Rejected);
                    break;

                default:
                    AssignProviderOperationRef(outcome.ProviderOperationRef);
                    MarkAll(FulfillmentReservationStatus.Unknown, ReservationMemberStatus.Unknown);
                    break;
            }

            LastObservedAt = observedAt;
        }

        public bool HoldLapsedAt(DateTimeOffset now) => ExpiresAt <= now;

        public bool ValidationIsStaleAt(DateTimeOffset now) => ReservationValidationTimeLimit <= now;

        public bool IsConfirmableAt(DateTimeOffset now, DateTimeOffset? lastTicketingDate)
            => Status == FulfillmentReservationStatus.Held
               && !HoldLapsedAt(now)
               && !ValidationIsStaleAt(now)
               && !(lastTicketingDate <= now);

        public ReleaseIntent PrepareRelease()
            => Status == FulfillmentReservationStatus.Held && ProviderOperationRef is { } providerOperationRef
                ? new ReleaseIntent(FulfillmentProviderKey, providerOperationRef, $"release-hold:{Id}")
                : throw ExceptionFactory.ReservationIsNotReleasable(Id, Status);

        public void RecordReleased(DateTimeOffset observedAt)
        {
            if (Status != FulfillmentReservationStatus.Held)
                throw ExceptionFactory.ReservationIsNotReleasable(Id, Status);

            MarkAll(FulfillmentReservationStatus.Released, ReservationMemberStatus.Released);
            LastObservedAt = observedAt;
        }

        public void RecordExpired(DateTimeOffset observedAt)
        {
            if (Status != FulfillmentReservationStatus.Held)
                throw ExceptionFactory.ReservationOutcomeCannotBeRecorded(Id, Status);

            MarkAll(FulfillmentReservationStatus.Expired, ReservationMemberStatus.Expired);
            LastObservedAt = observedAt;
        }

        private void AssignProviderOperationRef(string? providerOperationRef)
        {
            if (providerOperationRef is null)
                return;

            if (ProviderOperationRef is not null && ProviderOperationRef != providerOperationRef)
                throw ExceptionFactory.ProviderReferenceCannotChange(nameof(FulfillmentReservation), Id, ProviderOperationRef, providerOperationRef);

            ProviderOperationRef = providerOperationRef;
        }

        private void ApplyUnitOutcomes(IReadOnlyList<ReservationUnitOutcome> outcomes)
        {
            var outcomesByKey = outcomes.ToDictionary(outcome => outcome.UnitCorrelationKey, StringComparer.Ordinal);

            if (outcomesByKey.Count != _units.Count || _units.Any(unit => !outcomesByKey.ContainsKey(unit.UnitCorrelationKey)))
                throw ExceptionFactory.ReservationOutcomeDoesNotMatchUnits(Id);

            foreach (var unit in _units)
            {
                var outcome = outcomesByKey[unit.UnitCorrelationKey];
                unit.Record(outcome.Status, outcome.ProviderUnitRef, outcome.RawStatusCode, outcome.ObservedSeat);
            }
        }

        private FulfillmentReservationStatus StatusFromUnits()
        {
            var statuses = _units.Select(unit => unit.Status).Distinct().ToList();

            return statuses.Count == 1 ? ToReservationStatus(statuses[0]) : FulfillmentReservationStatus.Mixed;
        }

        private void MarkAll(FulfillmentReservationStatus reservationStatus, ReservationMemberStatus unitStatus)
        {
            Status = reservationStatus;

            foreach (var unit in _units)
                unit.Mark(unitStatus);
        }

        private static FulfillmentReservationStatus ToReservationStatus(ReservationMemberStatus status) => status switch
        {
            ReservationMemberStatus.Pending => FulfillmentReservationStatus.Pending,
            ReservationMemberStatus.Waitlisted => FulfillmentReservationStatus.Waitlisted,
            ReservationMemberStatus.Held => FulfillmentReservationStatus.Held,
            ReservationMemberStatus.Confirmed => FulfillmentReservationStatus.Confirmed,
            ReservationMemberStatus.Rejected => FulfillmentReservationStatus.Rejected,
            ReservationMemberStatus.Released => FulfillmentReservationStatus.Released,
            ReservationMemberStatus.Expired => FulfillmentReservationStatus.Expired,
            ReservationMemberStatus.Cancelled => FulfillmentReservationStatus.Cancelled,
            _ => FulfillmentReservationStatus.Unknown
        };
    }
}
