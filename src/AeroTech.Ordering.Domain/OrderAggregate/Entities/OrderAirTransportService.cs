using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderAirTransportService : OrderService
    {
        private OrderAirTransportService()
        {
        }

        public OrderAirTransportService(
            long id,
            long orderId,
            long orderItemId,
            long travellerId,
            long segmentId,
            long flightCapacityId,
            long? airFareId,
            string? bookingClass,
            string? fareBasis,
            string? fareFamily,
            string? fareType,
            BaggageAllowance? checkedBaggageAllowance,
            BaggageAllowance? cabinBaggageAllowance,
            bool isRefundable,
            bool isChangeable,
            bool isUpgradable,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : base(id, orderId, orderItemId, travellerId, OrderServiceType.AirTransportation, createdByChangeId, createdAt)
        {
            SegmentId = segmentId;
            FlightCapacityId = flightCapacityId;
            AirFareId = airFareId;
            BookingClass = bookingClass;
            FareBasis = fareBasis;
            FareFamily = fareFamily;
            FareType = fareType;
            CheckedBaggageAllowance = checkedBaggageAllowance;
            CabinBaggageAllowance = cabinBaggageAllowance;
            IsRefundable = isRefundable;
            IsChangeable = isChangeable;
            IsUpgradable = isUpgradable;
        }

        public long SegmentId { get; private set; }

        public long FlightCapacityId { get; private set; }

        public long? AirFareId { get; private set; }

        public string? BookingClass { get; private set; }

        public string? FareBasis { get; private set; }

        public string? FareFamily { get; private set; }

        public string? FareType { get; private set; }

        public BaggageAllowance? CheckedBaggageAllowance { get; private set; }

        public BaggageAllowance? CabinBaggageAllowance { get; private set; }

        public bool IsRefundable { get; private set; }

        public bool IsChangeable { get; private set; }

        public bool IsUpgradable { get; private set; }
    }
}
