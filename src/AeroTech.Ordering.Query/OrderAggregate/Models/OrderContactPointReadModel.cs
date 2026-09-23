using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderContactPointReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long OrderContactId { get; set; }

        public ContactPointType Type { get; set; }

        public string Value { get; set; } = default!;

        public string? CountryCode { get; set; }

        public bool IsPrimary { get; set; }
    }
}
