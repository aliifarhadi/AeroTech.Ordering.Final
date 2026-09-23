using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderChange : Entity<long>
    {
        private OrderChange()
        {
        }

        public OrderChange(
            long id,
            long orderId,
            OrderChangeType changeType,
            int commercialVersion,
            SalesContext actorContext,
            string? sourceReference,
            DateTimeOffset committedAt)
        {
            Id = id;
            OrderId = orderId;
            ChangeType = changeType;
            CommercialVersion = commercialVersion;
            ActorContext = actorContext;
            SourceReference = sourceReference;
            CommittedAt = committedAt;
        }

        public long OrderId { get; private set; }

        public OrderChangeType ChangeType { get; private set; }

        public int CommercialVersion { get; private set; }

        public SalesContext ActorContext { get; private set; } = default!;

        public string? SourceReference { get; private set; }

        public DateTimeOffset CommittedAt { get; private set; }
    }
}
