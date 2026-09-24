using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities
{
    public sealed class FulfillmentTaskAttempt : Entity<long>
    {
        private FulfillmentTaskAttempt()
        {
        }

        internal FulfillmentTaskAttempt(long id, long fulfillmentTaskId, int attemptNumber, DateTimeOffset startedAt)
        {
            Id = id;
            FulfillmentTaskId = fulfillmentTaskId;
            AttemptNumber = attemptNumber;
            StartedAt = startedAt;
        }

        public long FulfillmentTaskId { get; private set; }

        public int AttemptNumber { get; private set; }

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        public FulfillmentAttemptOutcome? Outcome { get; private set; }

        public FulfillmentFailureKind? FailureKind { get; private set; }

        public FulfillmentFailureReason? FailureReason { get; private set; }

        public string? Error { get; private set; }

        public bool IsInProgress => CompletedAt is null;

        internal void Complete(
            FulfillmentAttemptOutcome outcome,
            FulfillmentFailureKind? failureKind,
            FulfillmentFailureReason? failureReason,
            string? error,
            DateTimeOffset completedAt)
        {
            Outcome = outcome;
            FailureKind = failureKind;
            FailureReason = failureReason;
            Error = error;
            CompletedAt = completedAt;
        }
    }
}
