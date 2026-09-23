using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderItemReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public ProductType Kind { get; set; }

        public decimal AcceptedTotal { get; set; }

        public OrderItemCommercialState CommercialStatus { get; set; }
    }
}
