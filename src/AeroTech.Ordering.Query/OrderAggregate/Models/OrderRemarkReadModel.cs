using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderRemarkReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public OrderRemarkType Type { get; set; }

        public OrderRemarkVisibility Visibility { get; set; }

        public OrderRemarkScope Scope { get; set; }

        public long? TravellerId { get; set; }

        public long? SegmentId { get; set; }

        public long? OrderItemId { get; set; }

        public long? OrderServiceId { get; set; }

        public string Text { get; set; } = default!;

        public string? CategoryCode { get; set; }

        public bool IsPrintedOnItinerary { get; set; }

        public bool IsPrintedOnInvoice { get; set; }

        public OrderRemarkStatus Status { get; set; }

        public long? SupersedesRemarkId { get; set; }

        public long CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
