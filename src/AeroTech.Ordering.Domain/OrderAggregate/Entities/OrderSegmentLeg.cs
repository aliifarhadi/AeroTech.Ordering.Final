using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.AirPrice.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderSegmentLeg : Entity<long>
    {
        private OrderSegmentLeg()
        {
        }

        public OrderSegmentLeg(
            long id,
            long orderSegmentId,
            int sequence,
            long legId,
            int originAirportId,
            int? originAirportTerminalId,
            int destinationAirportId,
            int? destinationAirportTerminalId,
            DateTimeOffset departureDateTime,
            DateTimeOffset arrivalDateTime,
            StopType? stopType,
            int? stopDurationMinutes,
            bool? stopPassengersCanBoardOrLeave)
        {
            Id = id;
            OrderSegmentId = orderSegmentId;
            Sequence = sequence;
            LegId = legId;
            OriginAirportId = originAirportId;
            OriginAirportTerminalId = originAirportTerminalId;
            DestinationAirportId = destinationAirportId;
            DestinationAirportTerminalId = destinationAirportTerminalId;
            DepartureDateTime = departureDateTime;
            ArrivalDateTime = arrivalDateTime;
            StopType = stopType;
            StopDurationMinutes = stopDurationMinutes;
            StopPassengersCanBoardOrLeave = stopPassengersCanBoardOrLeave;
        }

        public long OrderSegmentId { get; private set; }

        public int Sequence { get; private set; }

        public long LegId { get; private set; }

        public int OriginAirportId { get; private set; }

        public int? OriginAirportTerminalId { get; private set; }

        public int DestinationAirportId { get; private set; }

        public int? DestinationAirportTerminalId { get; private set; }

        public DateTimeOffset DepartureDateTime { get; private set; }

        public DateTimeOffset ArrivalDateTime { get; private set; }

        public StopType? StopType { get; private set; }

        public int? StopDurationMinutes { get; private set; }

        public bool? StopPassengersCanBoardOrLeave { get; private set; }
    }
}
