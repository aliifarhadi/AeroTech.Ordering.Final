namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderSegmentReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long OrderJourneyId { get; set; }

        public int Sequence { get; set; }

        public long FlightId { get; set; }

        public string? FlightNumber { get; set; }

        public int MarketingAirlineId { get; set; }

        public int OperatingAirlineId { get; set; }

        public int OriginAirportId { get; set; }

        public int DestinationAirportId { get; set; }

        public DateTimeOffset SoldDeparture { get; set; }

        public DateTimeOffset SoldArrival { get; set; }

        public int Duration { get; set; }
    }
}
