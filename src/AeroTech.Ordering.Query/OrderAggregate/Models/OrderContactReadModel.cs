using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderContactReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public int Sequence { get; set; }

        public ContactRole Role { get; set; }

        public string? ContactName { get; set; }
    }
}
