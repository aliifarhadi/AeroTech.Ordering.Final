using AeroTech.Framework.Core.Domain.Entities;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderJourney : Entity<long>
    {
        private OrderJourney()
        {
        }

        public OrderJourney(
            long id,
            long orderId,
            int sequence,
            string boundId,
            int originAirportId,
            int destinationAirportId)
        {
            Id = id;
            OrderId = orderId;
            Sequence = sequence;
            BoundId = boundId;
            OriginAirportId = originAirportId;
            DestinationAirportId = destinationAirportId;
        }

        public long OrderId { get; private set; }

        public int Sequence { get; private set; }

        public string BoundId { get; private set; } = default!;

        public int OriginAirportId { get; private set; }

        public int DestinationAirportId { get; private set; }
    }
}
