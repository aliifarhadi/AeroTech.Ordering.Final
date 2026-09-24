using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderReadModel
    {
        public long Id { get; set; }

        public Guid OrderReference { get; set; }

        public string? RecordLocator { get; set; }

        public string SourceOfferId { get; set; } = default!;

        public OrderStatus Status { get; set; }

        public SalesChannel Channel { get; set; }

        public long CustomerId { get; set; }

        public long ActorId { get; set; }

        public long? TravelAgencyId { get; set; }

        public SellingOfficeKind? OfficeKind { get; set; }

        public long? OfficeId { get; set; }

        public int CurrencyId { get; set; }

        public decimal CustomerTotal { get; set; }

        public int CommercialVersion { get; set; }

        public DateTimeOffset? LastTicketingDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset LastProjectedAt { get; set; }
    }
}
