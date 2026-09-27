using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Models
{
    public sealed class DocumentStockReadModel
    {
        public long Id { get; set; }

        public int OwnerAirlineId { get; set; }

        public long? OfficeId { get; set; }

        public AccountableDocumentKind DocumentKind { get; set; }

        public string Prefix { get; set; } = default!;

        public int SerialWidth { get; set; }

        public string CheckDigitProfile { get; set; } = default!;

        public long RangeFrom { get; set; }

        public long RangeTo { get; set; }

        public long NextNumber { get; set; }

        public DocumentStockStatus Status { get; set; }
    }
}
