using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderAirportService : OrderAncillaryService
    {
        private OrderAirportService()
        {
        }

        public OrderAirportService(
            OrderAncillaryServiceArgs args,
            AirportServiceKind kind,
            int airportId,
            string? terminalRef,
            long? facilityId,
            AirportServiceDirection direction,
            DateTimeOffset? timeWithOffset,
            int? guestCount)
            : base(args, AncillaryProfile.AirportService)
        {
            EnsureValid(airportId > 0, nameof(AirportId));
            EnsureValid(guestCount is null or >= 0, nameof(GuestCount));

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
    }
}
