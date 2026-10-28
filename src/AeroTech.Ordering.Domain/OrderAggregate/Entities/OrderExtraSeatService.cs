using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderExtraSeatService : OrderAncillaryService
    {
        private OrderExtraSeatService()
        {
        }

        public OrderExtraSeatService(
            OrderAncillaryServiceArgs args,
            SeatPurpose seatPurpose,
            ExtraSeatPurpose? extraSeatPurpose,
            int? extraOccupiedSeatCount,
            string? seatNumber)
            : base(args, AncillaryProfile.Seat)
        {
            EnsureValid(extraOccupiedSeatCount is null or > 0, nameof(ExtraOccupiedSeatCount));

            SeatPurpose = seatPurpose;
            ExtraSeatPurpose = extraSeatPurpose;
            ExtraOccupiedSeatCount = extraOccupiedSeatCount;
            SeatNumber = string.IsNullOrWhiteSpace(seatNumber) ? throw MissingChoice(nameof(SeatNumber)) : seatNumber;
        }

        public SeatPurpose SeatPurpose { get; private set; }

        public ExtraSeatPurpose? ExtraSeatPurpose { get; private set; }

        public int? ExtraOccupiedSeatCount { get; private set; }

        public string SeatNumber { get; private set; } = default!;
    }
}
