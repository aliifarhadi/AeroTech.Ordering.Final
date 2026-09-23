using AeroTech.Framework.Core.Domain.Entities;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class PricingAllocation : Entity<long>
    {
        private PricingAllocation()
        {
        }

        public PricingAllocation(
            long id,
            long pricingLineId,
            long? orderItemId,
            long? orderServiceId,
            long? orderJourneyId,
            long? orderSegmentId,
            long? travellerId,
            decimal amount,
            int currencyId,
            decimal equivalentAmount,
            int equivalentCurrencyId)
        {
            Id = id;
            PricingLineId = pricingLineId;
            OrderItemId = orderItemId;
            OrderServiceId = orderServiceId;
            OrderJourneyId = orderJourneyId;
            OrderSegmentId = orderSegmentId;
            TravellerId = travellerId;
            Amount = amount;
            CurrencyId = currencyId;
            EquivalentAmount = equivalentAmount;
            EquivalentCurrencyId = equivalentCurrencyId;
        }

        public long PricingLineId { get; private set; }

        public long? OrderItemId { get; private set; }

        public long? OrderServiceId { get; private set; }

        public long? OrderJourneyId { get; private set; }

        public long? OrderSegmentId { get; private set; }

        public long? TravellerId { get; private set; }

        public decimal Amount { get; private set; }

        public int CurrencyId { get; private set; }

        public decimal EquivalentAmount { get; private set; }

        public int EquivalentCurrencyId { get; private set; }
    }
}
