using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities
{
    public sealed class ProviderInteraction : Entity<long>
    {
        private ProviderInteraction()
        {
        }

        internal ProviderInteraction(
            long id,
            long fulfillmentTaskId,
            int attemptNumber,
            ProviderInteractionType interactionType,
            DateTimeOffset startedAt)
        {
            Id = id;
            FulfillmentTaskId = fulfillmentTaskId;
            AttemptNumber = attemptNumber;
            InteractionType = interactionType;
            Status = ProviderInteractionStatus.Pending;
            StartedAt = startedAt;
        }

        public long FulfillmentTaskId { get; private set; }

        public int AttemptNumber { get; private set; }

        public ProviderInteractionType InteractionType { get; private set; }

        public ProviderInteractionStatus Status { get; private set; }

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        public int? ProviderStatusCode { get; private set; }

        public string? Error { get; private set; }

        internal void Complete(
            ProviderInteractionStatus status,
            int? providerStatusCode,
            string? error,
            DateTimeOffset completedAt)
        {
            Status = status;
            ProviderStatusCode = providerStatusCode;
            Error = error;
            CompletedAt = completedAt;
        }
    }
}
