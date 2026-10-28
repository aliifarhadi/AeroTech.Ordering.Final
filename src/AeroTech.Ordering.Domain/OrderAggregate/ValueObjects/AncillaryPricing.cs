using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AncillaryPricing : ValueObject
    {
        private AncillaryPricing()
        {
        }

        public AncillaryPricing(
            long pricingId,
            long pricingRateId,
            int pricingVersion,
            PricingUnit pricingUnit)
        {
            PricingId = pricingId;
            PricingRateId = pricingRateId;
            PricingVersion = pricingVersion;
            PricingUnit = pricingUnit;
        }

        public long PricingId { get; private set; }

        public long PricingRateId { get; private set; }

        public int PricingVersion { get; private set; }

        public PricingUnit PricingUnit { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return PricingId;
            yield return PricingRateId;
            yield return PricingVersion;
            yield return PricingUnit;
        }
    }
}
