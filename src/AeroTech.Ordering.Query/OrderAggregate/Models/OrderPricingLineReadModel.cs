using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderPricingLineReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public OrderPricingReason Reason { get; set; }

        public PricingLineScope Scope { get; set; }

        public OrderPricingLineCategory Category { get; set; }

        public OrderPricingLineSubCategory SubCategory { get; set; }

        public OrderPricingLineDirection Direction { get; set; }

        public PricingLineTreatment Treatment { get; set; }

        public string? Code { get; set; }

        public string? Description { get; set; }

        public string? Reference { get; set; }

        public decimal Amount { get; set; }

        public int CurrencyId { get; set; }

        public decimal EquivalentAmount { get; set; }

        public int EquivalentCurrencyId { get; set; }

        public RefundabilityRule? Refundability { get; set; }
    }
}
