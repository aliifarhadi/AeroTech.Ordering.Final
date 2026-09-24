using AeroTech.Framework.Core.Domain.Entities;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderFareComponent : Entity<long>
    {
        private readonly List<long> _coveredOrderServiceIds = new();

        private OrderFareComponent()
        {
        }

        internal OrderFareComponent(
            long id,
            long orderFarePricingUnitId,
            int sequence,
            long airFareId,
            string? bookingClass,
            string? fareBasis,
            string? fareFamily,
            string? fareType,
            IReadOnlyCollection<long> coveredOrderServiceIds)
        {
            Id = id;
            OrderFarePricingUnitId = orderFarePricingUnitId;
            Sequence = sequence;
            AirFareId = airFareId;
            BookingClass = bookingClass;
            FareBasis = fareBasis;
            FareFamily = fareFamily;
            FareType = fareType;
            _coveredOrderServiceIds.AddRange(coveredOrderServiceIds);
        }

        public long OrderFarePricingUnitId { get; private set; }

        public int Sequence { get; private set; }

        public long AirFareId { get; private set; }

        public string? BookingClass { get; private set; }

        public string? FareBasis { get; private set; }

        public string? FareFamily { get; private set; }

        public string? FareType { get; private set; }

        public IReadOnlyCollection<long> CoveredOrderServiceIds => _coveredOrderServiceIds.AsReadOnly();
    }
}
