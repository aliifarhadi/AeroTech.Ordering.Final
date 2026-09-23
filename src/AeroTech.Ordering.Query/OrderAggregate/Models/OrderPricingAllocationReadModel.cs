namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderPricingAllocationReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long PricingLineId { get; set; }

        public long? OrderItemId { get; set; }

        public long? OrderServiceId { get; set; }

        public long? OrderJourneyId { get; set; }

        public long? OrderSegmentId { get; set; }

        public long? TravellerId { get; set; }

        public decimal Amount { get; set; }

        public int CurrencyId { get; set; }

        public decimal EquivalentAmount { get; set; }

        public int EquivalentCurrencyId { get; set; }
    }
}
