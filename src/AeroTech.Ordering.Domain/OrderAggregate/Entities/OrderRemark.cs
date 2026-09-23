using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderRemark : Entity<long>
    {
        private OrderRemark()
        {
        }

        public OrderRemark(
            long id,
            long orderId,
            OrderRemarkType type,
            OrderRemarkVisibility visibility,
            OrderRemarkScope scope,
            long? travellerId,
            long? segmentId,
            long? orderItemId,
            long? orderServiceId,
            string text,
            string? categoryCode,
            bool isPrintedOnItinerary,
            bool isPrintedOnInvoice,
            long? supersedesRemarkId,
            long createdBy,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderId = orderId;
            Type = type;
            Visibility = visibility;
            Scope = scope;
            TravellerId = travellerId;
            SegmentId = segmentId;
            OrderItemId = orderItemId;
            OrderServiceId = orderServiceId;
            Text = text;
            CategoryCode = categoryCode;
            IsPrintedOnItinerary = isPrintedOnItinerary;
            IsPrintedOnInvoice = isPrintedOnInvoice;
            Status = OrderRemarkStatus.Active;
            SupersedesRemarkId = supersedesRemarkId;
            CreatedBy = createdBy;
            CreatedAt = createdAt;
        }

        public long OrderId { get; private set; }

        public OrderRemarkType Type { get; private set; }

        public OrderRemarkVisibility Visibility { get; private set; }

        public OrderRemarkScope Scope { get; private set; }

        public long? TravellerId { get; private set; }

        public long? SegmentId { get; private set; }

        public long? OrderItemId { get; private set; }

        public long? OrderServiceId { get; private set; }

        public string Text { get; private set; } = default!;

        public string? CategoryCode { get; private set; }

        public bool IsPrintedOnItinerary { get; private set; }

        public bool IsPrintedOnInvoice { get; private set; }

        public OrderRemarkStatus Status { get; private set; }

        public long? SupersedesRemarkId { get; private set; }

        public long CreatedBy { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        internal void Supersede() => Status = OrderRemarkStatus.Superseded;
    }
}
