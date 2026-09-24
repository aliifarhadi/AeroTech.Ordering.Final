using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;

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
            string fulfillmentProviderKey,
            long createdByChangeId,
            DateTimeOffset createdAt)
        {
            if (string.IsNullOrWhiteSpace(fulfillmentProviderKey))
                throw ExceptionFactory.FulfillmentProviderIsRequired();

            Id = id;
            OrderId = orderId;
            OrderItemId = orderItemId;
            TravellerId = travellerId;
            ServiceType = serviceType;
            FulfillmentProviderKey = fulfillmentProviderKey;
            CommercialStatus = OrderServiceCommercialState.Active;
            CreatedByChangeId = createdByChangeId;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public long OrderItemId { get; private set; }

        public long TravellerId { get; private set; }

        public OrderServiceType ServiceType { get; private set; }

        public string FulfillmentProviderKey { get; private set; } = default!;

        public OrderServiceCommercialState CommercialStatus { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
    }
}
