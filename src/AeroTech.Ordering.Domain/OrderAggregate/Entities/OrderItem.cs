using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderItem : Entity<long>
    {
        private OrderItem()
        {
        }

        public OrderItem(long id, long orderId, ProductType kind, long createdByChangeId, DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            Kind = kind;
            AcceptedTotal = 0m;
            CommercialStatus = OrderItemCommercialState.Active;
            CreatedByChangeId = createdByChangeId;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public ProductType Kind { get; private set; }

        public decimal AcceptedTotal { get; private set; }

        public OrderItemCommercialState CommercialStatus { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        internal void SetAcceptedTotal(decimal acceptedTotal) => AcceptedTotal = acceptedTotal;
    }
}
