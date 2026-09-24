using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderSeatService : OrderService
    {
        private OrderSeatService()
        {
        }

        public OrderSeatService(
            long id,
            long orderId,
            long orderItemId,
            long travellerId,
            long segmentId,
            long associatedAirServiceId,
            string seatNumber,
            string fulfillmentProviderKey,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : base(id, orderId, orderItemId, travellerId, OrderServiceType.SeatAssignment, fulfillmentProviderKey, createdByChangeId, createdAt)
        {
            SegmentId = segmentId;
            AssociatedAirServiceId = associatedAirServiceId;
            SeatNumber = seatNumber;
        }

        public long SegmentId { get; private set; }

        public long AssociatedAirServiceId { get; private set; }

        public string SeatNumber { get; private set; } = default!;
    }
}
