using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects
{
    public sealed class ReservationValidationEvidence : ValueObject
    {
        private readonly List<long> _validatedOrderServiceIds = new();

        private ReservationValidationEvidence()
        {
        }

        public ReservationValidationEvidence(
            int commercialVersion,
            DateTimeOffset validUntil,
            DateTimeOffset validatedAt,
            IReadOnlyCollection<long> validatedOrderServiceIds)
        {
            if (commercialVersion <= 0
                || validatedOrderServiceIds.Count == 0
                || validatedOrderServiceIds.Distinct().Count() != validatedOrderServiceIds.Count)
                throw ExceptionFactory.ReservationValidationEvidenceIsInvalid(commercialVersion);

            CommercialVersion = commercialVersion;
            ValidUntil = validUntil;
            ValidatedAt = validatedAt;
            _validatedOrderServiceIds.AddRange(validatedOrderServiceIds);
        }

        public int CommercialVersion { get; private set; }

        public DateTimeOffset ValidUntil { get; private set; }

        public DateTimeOffset ValidatedAt { get; private set; }

        public IReadOnlyCollection<long> ValidatedOrderServiceIds => _validatedOrderServiceIds.AsReadOnly();

        public bool Covers(IEnumerable<long> orderServiceIds, int commercialVersion, DateTimeOffset now)
            => CommercialVersion == commercialVersion
               && now < ValidUntil
               && orderServiceIds.All(_validatedOrderServiceIds.Contains);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return CommercialVersion;
            yield return ValidUntil;
            yield return ValidatedAt;

            foreach (var orderServiceId in _validatedOrderServiceIds.Order())
                yield return orderServiceId;
        }
    }
}
