using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class PricingLine : Entity<long>
    {
        private readonly List<PricingAllocation> _allocations = new();

        private PricingLine()
        {
        }

        public PricingLine(
            long id,
            long orderId,
            long createdByChangeId,
            OrderPricingReason reason,
            PricingLineScope scope,
            OrderPricingLineCategory category,
            OrderPricingLineSubCategory subCategory,
            OrderPricingLineDirection direction,
            PricingLineTreatment treatment,
            string? code,
            string? description,
            string? reference,
            decimal amount,
            int currencyId,
            decimal equivalentAmount,
            int equivalentCurrencyId,
            ExchangeRateSnapshot? exchangeRateSnapshot,
            RefundabilityRule? refundability,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            CreatedByChangeId = createdByChangeId;
            Reason = reason;
            Scope = scope;
            Category = category;
            SubCategory = subCategory;
            Direction = direction;
            Treatment = treatment;
            Code = code;
            Description = description;
            Reference = reference;
            Amount = amount;
            CurrencyId = currencyId;
            EquivalentAmount = equivalentAmount;
            EquivalentCurrencyId = equivalentCurrencyId;
            ExchangeRateSnapshot = exchangeRateSnapshot;
            Refundability = refundability;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public OrderPricingReason Reason { get; private set; }

        public PricingLineScope Scope { get; private set; }

        public OrderPricingLineCategory Category { get; private set; }

        public OrderPricingLineSubCategory SubCategory { get; private set; }

        public OrderPricingLineDirection Direction { get; private set; }

        public PricingLineTreatment Treatment { get; private set; }

        public string? Code { get; private set; }

        public string? Description { get; private set; }

        public string? Reference { get; private set; }

        public decimal Amount { get; private set; }

        public int CurrencyId { get; private set; }

        public decimal EquivalentAmount { get; private set; }

        public int EquivalentCurrencyId { get; private set; }

        public ExchangeRateSnapshot? ExchangeRateSnapshot { get; private set; }

        public RefundabilityRule? Refundability { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public IReadOnlyCollection<PricingAllocation> Allocations => _allocations.AsReadOnly();

        public decimal SignedEquivalentAmount => Direction == OrderPricingLineDirection.Credit
            ? EquivalentAmount
            : -EquivalentAmount;

        internal void AddAllocation(PricingAllocation allocation) => _allocations.Add(allocation);
    }
}
