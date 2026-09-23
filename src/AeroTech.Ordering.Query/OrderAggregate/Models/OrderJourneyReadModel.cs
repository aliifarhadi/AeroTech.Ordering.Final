namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderJourneyReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public int Sequence { get; set; }

        public string BoundId { get; set; } = default!;

        public int OriginAirportId { get; set; }

        public int DestinationAirportId { get; set; }
    }
}
