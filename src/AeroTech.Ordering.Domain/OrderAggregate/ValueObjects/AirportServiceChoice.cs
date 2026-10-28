using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AirportServiceChoice : ValueObject
    {
        private AirportServiceChoice()
        {
        }

        public AirportServiceChoice(
            AirportServiceKind kind,
            int airportId,
            string? terminalRef,
            long? facilityId,
            AirportServiceDirection direction,
            DateTimeOffset? timeWithOffset,
            int? guestCount)
        {
            Kind = kind;
            AirportId = airportId;
            TerminalRef = terminalRef;
            FacilityId = facilityId;
            Direction = direction;
            TimeWithOffset = timeWithOffset;
            GuestCount = guestCount;
        }

        public AirportServiceKind Kind { get; private set; }

        public int AirportId { get; private set; }

        public string? TerminalRef { get; private set; }

        public long? FacilityId { get; private set; }

        public AirportServiceDirection Direction { get; private set; }

        public DateTimeOffset? TimeWithOffset { get; private set; }

        public int? GuestCount { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Kind;
            yield return AirportId;
            yield return TerminalRef;
            yield return FacilityId;
            yield return Direction;
            yield return TimeWithOffset;
            yield return GuestCount;
        }
    }
}
