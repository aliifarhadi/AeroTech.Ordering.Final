using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class PriorityChoice : ValueObject
    {
        private PriorityChoice()
        {
        }

        public PriorityChoice(
            PriorityKind kind,
            string? priorityZoneCode,
            string? priorityGroupCode,
            string? fareBenefitRef,
            bool? optIn)
        {
            Kind = kind;
            PriorityZoneCode = priorityZoneCode;
            PriorityGroupCode = priorityGroupCode;
            FareBenefitRef = fareBenefitRef;
            OptIn = optIn;
        }

        public PriorityKind Kind { get; private set; }

        public string? PriorityZoneCode { get; private set; }

        public string? PriorityGroupCode { get; private set; }

        public string? FareBenefitRef { get; private set; }

        public bool? OptIn { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Kind;
            yield return PriorityZoneCode;
            yield return PriorityGroupCode;
            yield return FareBenefitRef;
            yield return OptIn;
        }
    }
}
