using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class UnaccompaniedMinorChoice : ValueObject
    {
        private UnaccompaniedMinorChoice()
        {
        }

        public UnaccompaniedMinorChoice(
            string guardianHandoffContact,
            string guardianPickupContact)
        {
            GuardianHandoffContact = guardianHandoffContact;
            GuardianPickupContact = guardianPickupContact;
        }

        public string GuardianHandoffContact { get; private set; } = default!;

        public string GuardianPickupContact { get; private set; } = default!;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return GuardianHandoffContact;
            yield return GuardianPickupContact;
        }
    }
}
