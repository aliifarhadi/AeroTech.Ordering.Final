using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderServiceReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long OrderItemId { get; set; }

        public long TravellerId { get; set; }

        public long SegmentId { get; set; }

        public OrderServiceType ServiceType { get; set; }

        public OrderServiceCommercialState CommercialStatus { get; set; }

        public string? BookingClass { get; set; }

        public string? FareBasis { get; set; }

        public string? FareFamily { get; set; }

        public string? FareType { get; set; }

        public int? CheckedBaggagePieces { get; set; }

        public decimal? CheckedBaggageWeight { get; set; }

        public WeightUnit? CheckedBaggageUnit { get; set; }

        public int? CabinBaggagePieces { get; set; }

        public decimal? CabinBaggageWeight { get; set; }

        public WeightUnit? CabinBaggageUnit { get; set; }

        public bool IsRefundable { get; set; }

        public bool IsChangeable { get; set; }

        public bool IsUpgradable { get; set; }

        public string? SeatNumber { get; set; }
    }
}
