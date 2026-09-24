using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.FulfillmentTaskAggregate
{
    public sealed class FulfillmentTask : AggregateRoot<long>
    {
        private const int MaxErrorLength = 2000;
        private const string InterruptedAttemptError = "The attempt ended before its outcome was recorded.";

        private readonly List<FulfillmentTaskTarget> _targets = new();
        private readonly List<FulfillmentTaskAttempt> _attempts = new();
        private readonly List<ProviderInteraction> _interactions = new();

        private FulfillmentTask()
        {
        }

        private FulfillmentTask(
            long id,
            long orderId,
            long fulfillmentReservationId,
            OrderFulfillmentTaskType taskType,
            string fulfillmentProviderKey,
            string idempotencyKey,
            string correlationReference,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            FulfillmentReservationId = fulfillmentReservationId;
            TaskType = taskType;
            FulfillmentProviderKey = fulfillmentProviderKey;
            IdempotencyKey = idempotencyKey;
            CorrelationReference = correlationReference;
            Status = OrderFulfillmentStatus.Pending;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public long FulfillmentReservationId { get; private set; }

        public OrderFulfillmentTaskType TaskType { get; private set; }

        public string FulfillmentProviderKey { get; private set; } = default!;

        public string IdempotencyKey { get; private set; } = default!;

        public string CorrelationReference { get; private set; } = default!;

        public OrderFulfillmentStatus Status { get; private set; }

        public int AttemptCount { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        public FulfillmentFailureKind? LastFailureKind { get; private set; }

        public FulfillmentFailureReason? LastFailureReason { get; private set; }

        public string? LastError { get; private set; }

        public IReadOnlyCollection<FulfillmentTaskTarget> Targets => _targets.AsReadOnly();

        public IReadOnlyCollection<FulfillmentTaskAttempt> Attempts => _attempts.AsReadOnly();

        public IReadOnlyCollection<ProviderInteraction> Interactions => _interactions.AsReadOnly();

        public bool IsResumable => Status is OrderFulfillmentStatus.Pending
            or OrderFulfillmentStatus.InProgress
            or OrderFulfillmentStatus.Unknown;

        public static FulfillmentTask Create(
            long id,
            long orderId,
            long fulfillmentReservationId,
            OrderFulfillmentTaskType taskType,
            string fulfillmentProviderKey,
            string idempotencyKey,
            string correlationReference,
            IReadOnlyCollection<long> targetReservationUnitIds,
            OrderFulfillmentTargetAction action,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt)
        {
            var task = new FulfillmentTask(
                id,
                orderId,
                fulfillmentReservationId,
                taskType,
                fulfillmentProviderKey,
                idempotencyKey,
                correlationReference,
                createdAt);

            foreach (var reservationUnitId in targetReservationUnitIds)
                task._targets.Add(new FulfillmentTaskTarget(idGenerator.NewId(), id, reservationUnitId, action));

            return task;
        }

        public void StartAttempt(ProviderInteractionType interactionType, IIdGenerator idGenerator, DateTimeOffset startedAt)
        {
            if (!IsResumable)
                throw ExceptionFactory.FulfillmentTaskCannotStartAttempt(Id, Status);

            if (Status == OrderFulfillmentStatus.InProgress)
                CloseAttempt(
                    FulfillmentAttemptOutcome.Unknown,
                    new ProviderFailure(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, InterruptedAttemptError, null),
                    startedAt);

            AttemptCount++;
            _attempts.Add(new FulfillmentTaskAttempt(idGenerator.NewId(), Id, AttemptCount, startedAt));
            _interactions.Add(new ProviderInteraction(idGenerator.NewId(), Id, AttemptCount, interactionType, startedAt));
            Status = OrderFulfillmentStatus.InProgress;
        }

        public void CompleteAttempt(FulfillmentAttemptOutcome outcome, ProviderFailure? failure, DateTimeOffset completedAt)
        {
            if (Status != OrderFulfillmentStatus.InProgress)
                throw ExceptionFactory.FulfillmentTaskHasNoAttemptInProgress(Id);

            CloseAttempt(outcome, failure, completedAt);
        }

        private void CloseAttempt(FulfillmentAttemptOutcome outcome, ProviderFailure? failure, DateTimeOffset completedAt)
        {
            var attempt = _attempts.Single(item => item.AttemptNumber == AttemptCount);
            var interaction = _interactions.Single(item => item.AttemptNumber == AttemptCount);
            var error = Truncate(failure?.Message);

            attempt.Complete(outcome, failure?.Kind, failure?.Reason, error, completedAt);
            interaction.Complete(InteractionStatusFor(outcome, failure), failure?.ProviderStatusCode, error, completedAt);

            if (failure is not null)
            {
                LastFailureKind = failure.Kind;
                LastFailureReason = failure.Reason;
                LastError = error;
            }

            Status = outcome switch
            {
                FulfillmentAttemptOutcome.Succeeded => OrderFulfillmentStatus.Succeeded,
                FulfillmentAttemptOutcome.Failed => OrderFulfillmentStatus.Failed,
                _ => OrderFulfillmentStatus.Unknown
            };

            if (Status is OrderFulfillmentStatus.Succeeded or OrderFulfillmentStatus.Failed)
                CompletedAt = completedAt;
        }

        private static ProviderInteractionStatus InteractionStatusFor(FulfillmentAttemptOutcome outcome, ProviderFailure? failure)
        {
            if (outcome == FulfillmentAttemptOutcome.Succeeded)
                return ProviderInteractionStatus.Succeeded;

            return failure is { Kind: FulfillmentFailureKind.Indeterminate, ProviderStatusCode: null }
                ? ProviderInteractionStatus.TimedOut
                : ProviderInteractionStatus.Failed;
        }

        private static string? Truncate(string? value)
            => value is { Length: > MaxErrorLength } ? value[..MaxErrorLength] : value;
    }
}
