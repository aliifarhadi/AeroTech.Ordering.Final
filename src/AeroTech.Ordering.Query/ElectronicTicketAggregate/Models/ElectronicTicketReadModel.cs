using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Models;

namespace AeroTech.Ordering.Query.ElectronicTicketAggregate.Models
{
    public sealed class ElectronicTicketReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long TravellerId { get; set; }

        public string DocumentNumber { get; set; } = default!;

        public DocumentAuthority Authority { get; set; }

        public DateTimeOffset IssuedAt { get; set; }

        public decimal IssuedTotal { get; set; }

        public int CurrencyId { get; set; }

        public ElectronicTicketStatus StatusSummary { get; set; }

        public int DocumentVersion { get; set; }
    }
}
