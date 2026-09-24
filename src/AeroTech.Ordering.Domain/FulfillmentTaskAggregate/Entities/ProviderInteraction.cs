using System.Security.Cryptography;
using System.Text;
using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

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
            FulfillmentTaskAttempt attempt,
            int sequence,
            string fulfillmentProviderKey,
            ProviderRequest request,
            DateTimeOffset startedAt)
        {
            if (request.InteractionType != ProviderInteractionType.ReadReservation && string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw ExceptionFactory.ProviderMutationRequiresIdempotencyKey(request.InteractionType);

            Id = id;
            FulfillmentTaskId = fulfillmentTaskId;
            FulfillmentTaskAttemptId = attempt.Id;
            AttemptNumber = attempt.AttemptNumber;
            Sequence = sequence;
            FulfillmentProviderKey = fulfillmentProviderKey;
            InteractionType = request.InteractionType;
            IdempotencyKey = request.IdempotencyKey;
            CorrelationReference = request.CorrelationReference;
            RequestPayload = request.Payload;
            RequestHash = HashOf(request.Payload);
            Status = ProviderInteractionStatus.Pending;
            StartedAt = startedAt;
        }

        public long FulfillmentTaskId { get; private set; }

        public long FulfillmentTaskAttemptId { get; private set; }

        public int AttemptNumber { get; private set; }

        public int Sequence { get; private set; }

        public string FulfillmentProviderKey { get; private set; } = default!;

        public ProviderInteractionType InteractionType { get; private set; }

        public string? IdempotencyKey { get; private set; }

        public string? CorrelationReference { get; private set; }

        public string RequestPayload { get; private set; } = default!;

        public string RequestHash { get; private set; } = default!;

        public string? ResponsePayload { get; private set; }

        public string? ResponseHash { get; private set; }

        public string? ProviderOperationRef { get; private set; }

        public ProviderInteractionStatus Status { get; private set; }

        public DateTimeOffset StartedAt { get; private set; }

        public DateTimeOffset? CompletedAt { get; private set; }

        public int? ProviderStatusCode { get; private set; }

        public string? Error { get; private set; }

        public bool IsInFlight => Status == ProviderInteractionStatus.Pending;

        public ProviderRequest ToRequest() => new(InteractionType, IdempotencyKey, CorrelationReference, RequestPayload);

        internal void Complete(
            ProviderInteractionStatus status,
            string? providerOperationRef,
            ProviderResponse? response,
            string? error,
            DateTimeOffset completedAt)
        {
            Status = status;
            ProviderOperationRef = providerOperationRef;
            ProviderStatusCode = response?.StatusCode;
            ResponsePayload = response?.Payload;
            ResponseHash = response?.Payload is { } payload ? HashOf(payload) : null;
            Error = error;
            CompletedAt = completedAt;
        }

        private static string HashOf(string payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
