using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects
{
    public sealed class IssuedSegmentSnapshot : ValueObject
    {
        private IssuedSegmentSnapshot()
        {
        }

        public IssuedSegmentSnapshot(
            int marketingAirlineId,
            int operatingAirlineId,
            string? flightNumber,
            int originAirportId,
            int destinationAirportId,
            DateTimeOffset departureDateTime,
            DateTimeOffset arrivalDateTime,
            string? bookingClass,
            long? rbdId,
            long? cabinClassId,
            string? sourceSegmentReference)
        {
            MarketingAirlineId = marketingAirlineId;
            OperatingAirlineId = operatingAirlineId;
            FlightNumber = flightNumber;
            OriginAirportId = originAirportId;
            DestinationAirportId = destinationAirportId;
            DepartureDateTime = departureDateTime;
            ArrivalDateTime = arrivalDateTime;
            BookingClass = bookingClass;
            RbdId = rbdId;
            CabinClassId = cabinClassId;
            SourceSegmentReference = sourceSegmentReference;
        }

        public int MarketingAirlineId { get; private set; }

        public int OperatingAirlineId { get; private set; }

        public string? FlightNumber { get; private set; }

        public int OriginAirportId { get; private set; }

        public int DestinationAirportId { get; private set; }

        public DateTimeOffset DepartureDateTime { get; private set; }

        public DateTimeOffset ArrivalDateTime { get; private set; }

        public string? BookingClass { get; private set; }

        public long? RbdId { get; private set; }

        public long? CabinClassId { get; private set; }

        public string? SourceSegmentReference { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return MarketingAirlineId;
            yield return OperatingAirlineId;
            yield return FlightNumber;
            yield return OriginAirportId;
            yield return DestinationAirportId;
            yield return DepartureDateTime;
            yield return ArrivalDateTime;
            yield return BookingClass;
            yield return RbdId;
            yield return CabinClassId;
            yield return SourceSegmentReference;
        }
    }
}
