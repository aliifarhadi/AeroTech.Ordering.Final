using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AssistanceChoice : ValueObject
    {
        private AssistanceChoice()
        {
        }

        public AssistanceChoice(
            AssistanceKind assistanceKind,
            string? assistanceSsrCode,
            long? infantTravellerId,
            long? guardianTravellerId)
        {
            AssistanceKind = assistanceKind;
            AssistanceSsrCode = assistanceSsrCode;
            InfantTravellerId = infantTravellerId;
            GuardianTravellerId = guardianTravellerId;
        }

        public AssistanceKind AssistanceKind { get; private set; }

        public string? AssistanceSsrCode { get; private set; }

        public long? InfantTravellerId { get; private set; }

        public long? GuardianTravellerId { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return AssistanceKind;
            yield return AssistanceSsrCode;
            yield return InfantTravellerId;
            yield return GuardianTravellerId;
        }
    }
}
