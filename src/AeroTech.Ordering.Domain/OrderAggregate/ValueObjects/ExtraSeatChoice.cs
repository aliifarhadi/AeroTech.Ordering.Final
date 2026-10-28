using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class ExtraSeatChoice : ValueObject
    {
        private ExtraSeatChoice()
        {
        }

        public ExtraSeatChoice(
            SeatPurpose seatPurpose,
            ExtraSeatPurpose? extraSeatPurpose,
            int? extraOccupiedSeatCount,
            string seatNumber)
        {
            SeatPurpose = seatPurpose;
            ExtraSeatPurpose = extraSeatPurpose;
            ExtraOccupiedSeatCount = extraOccupiedSeatCount;
            SeatNumber = seatNumber;
        }

        public SeatPurpose SeatPurpose { get; private set; }

        public ExtraSeatPurpose? ExtraSeatPurpose { get; private set; }

        public int? ExtraOccupiedSeatCount { get; private set; }

        public string SeatNumber { get; private set; } = default!;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return SeatPurpose;
            yield return ExtraSeatPurpose;
            yield return ExtraOccupiedSeatCount;
            yield return SeatNumber;
        }
    }
}
