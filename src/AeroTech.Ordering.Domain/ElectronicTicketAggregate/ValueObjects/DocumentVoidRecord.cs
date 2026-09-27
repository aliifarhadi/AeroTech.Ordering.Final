using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects
{
    public sealed class DocumentVoidRecord : ValueObject
    {
        private DocumentVoidRecord()
        {
        }

        public DocumentVoidRecord(
            long voidFulfillmentTaskId,
            string? reasonCode,
            string? reasonText,
            string? providerReference,
            long? actorId,
            DateTimeOffset voidedAt)
        {
            VoidFulfillmentTaskId = voidFulfillmentTaskId;
            ReasonCode = reasonCode;
            ReasonText = reasonText;
            ProviderReference = providerReference;
            ActorId = actorId;
            VoidedAt = voidedAt;
        }

        public long VoidFulfillmentTaskId { get; private set; }

        public string? ReasonCode { get; private set; }

        public string? ReasonText { get; private set; }

        public string? ProviderReference { get; private set; }

        public long? ActorId { get; private set; }

        public DateTimeOffset VoidedAt { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return VoidFulfillmentTaskId;
            yield return ReasonCode;
            yield return ReasonText;
            yield return ProviderReference;
            yield return ActorId;
            yield return VoidedAt;
        }
    }
}
