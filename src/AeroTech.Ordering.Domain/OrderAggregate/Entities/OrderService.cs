using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public abstract class OrderService : Entity<long>
    {
        protected OrderService()
        {
        }

        protected OrderService(
            long id,
            long orderId,
            long orderItemId,
            long travellerId,
            OrderServiceType serviceType,
            long createdByChangeId,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            OrderItemId = orderItemId;
            TravellerId = travellerId;
            ServiceType = serviceType;
            CommercialStatus = OrderServiceCommercialState.Active;
            CreatedByChangeId = createdByChangeId;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public long OrderItemId { get; private set; }

        public long TravellerId { get; private set; }

        public OrderServiceType ServiceType { get; private set; }

        public OrderServiceCommercialState CommercialStatus { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
    }
}
