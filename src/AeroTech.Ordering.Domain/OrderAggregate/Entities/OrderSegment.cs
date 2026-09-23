using AeroTech.Framework.Core.Domain.Entities;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderSegment : Entity<long>
    {
        private readonly List<OrderSegmentLeg> _legs = new();

        private OrderSegment()
        {
        }

        public OrderSegment(
            long id,
            long orderId,
            long orderJourneyId,
            int sequence,
            long flightId,
            int flightVersion,
            string? flightNumber,
            int originAirportId,
            int? originAirportTerminalId,
            int destinationAirportId,
            int? destinationAirportTerminalId,
            int operatingAirlineId,
            int marketingAirlineId,
            DateTimeOffset soldDeparture,
            DateTimeOffset soldArrival,
            int duration,
            int? aircraftId)
        {
            Id = id;
            OrderId = orderId;
            OrderJourneyId = orderJourneyId;
            Sequence = sequence;
            FlightId = flightId;
            FlightVersion = flightVersion;
            FlightNumber = flightNumber;
            OriginAirportId = originAirportId;
            OriginAirportTerminalId = originAirportTerminalId;
            DestinationAirportId = destinationAirportId;
            DestinationAirportTerminalId = destinationAirportTerminalId;
            OperatingAirlineId = operatingAirlineId;
            MarketingAirlineId = marketingAirlineId;
            SoldDeparture = soldDeparture;
            SoldArrival = soldArrival;
            Duration = duration;
            AircraftId = aircraftId;
        }

        public long OrderId { get; private set; }

        public long OrderJourneyId { get; private set; }

        public int Sequence { get; private set; }

        public long FlightId { get; private set; }

        public int FlightVersion { get; private set; }

        public string? FlightNumber { get; private set; }

        public int OriginAirportId { get; private set; }

        public int? OriginAirportTerminalId { get; private set; }

        public int DestinationAirportId { get; private set; }

        public int? DestinationAirportTerminalId { get; private set; }

        public int OperatingAirlineId { get; private set; }

        public int MarketingAirlineId { get; private set; }

        public DateTimeOffset SoldDeparture { get; private set; }

        public DateTimeOffset SoldArrival { get; private set; }

        public int Duration { get; private set; }

        public int? AircraftId { get; private set; }

        public IReadOnlyCollection<OrderSegmentLeg> Legs => _legs.AsReadOnly();

        internal void AddLeg(OrderSegmentLeg leg) => _legs.Add(leg);
    }
}
