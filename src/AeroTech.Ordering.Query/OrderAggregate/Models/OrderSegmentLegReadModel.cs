namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderSegmentLegReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long OrderSegmentId { get; set; }

        public int Sequence { get; set; }

        public long LegId { get; set; }

        public int OriginAirportId { get; set; }

        public int DestinationAirportId { get; set; }

        public DateTimeOffset DepartureDateTime { get; set; }

        public DateTimeOffset ArrivalDateTime { get; set; }
    }
}
