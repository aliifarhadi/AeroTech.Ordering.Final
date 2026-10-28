using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderPriorityService : OrderAncillaryService
    {
        private OrderPriorityService()
        {
        }

        public OrderPriorityService(
            OrderAncillaryServiceArgs args,
            PriorityKind kind,
            string? priorityZoneCode,
            string? priorityGroupCode,
            string? fareBenefitRef,
            bool? optIn)
            : base(args, AncillaryProfile.Priority)
        {
            var acceptedOptIn = optIn ?? throw MissingChoice(nameof(OptIn));

            EnsureValid(acceptedOptIn, nameof(OptIn));

            Kind = kind;
            PriorityZoneCode = priorityZoneCode;
            PriorityGroupCode = priorityGroupCode;
            FareBenefitRef = fareBenefitRef;
            OptIn = acceptedOptIn;
        }

        public PriorityKind Kind { get; private set; }

        public string? PriorityZoneCode { get; private set; }

        public string? PriorityGroupCode { get; private set; }

        public string? FareBenefitRef { get; private set; }

        public bool OptIn { get; private set; }
    }
}
