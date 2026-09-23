using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderContact : Entity<long>
    {
        private readonly List<OrderContactPoint> _contactPoints = new();

        private OrderContact()
        {
        }

        public OrderContact(long id, long orderId, int sequence, ContactRole role, string? contactName)
        {
            Id = id;
            OrderId = orderId;
            Sequence = sequence;
            Role = role;
            ContactName = contactName;
        }

        public long OrderId { get; private set; }

        public int Sequence { get; private set; }

        public ContactRole Role { get; private set; }

        public string? ContactName { get; private set; }

        public IReadOnlyCollection<OrderContactPoint> ContactPoints => _contactPoints.AsReadOnly();

        internal void AddContactPoint(OrderContactPoint contactPoint) => _contactPoints.Add(contactPoint);
    }
}
