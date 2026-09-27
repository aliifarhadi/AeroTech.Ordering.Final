namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed class FarePricingAtom
    {
        internal FarePricingAtom(long orderFarePricingUnitId, IEnumerable<long> airServiceIds)
        {
            OrderFarePricingUnitId = orderFarePricingUnitId;
            AirServiceIds = airServiceIds.ToHashSet();
        }

        public long OrderFarePricingUnitId { get; }

        public IReadOnlySet<long> AirServiceIds { get; }

        public bool IsFracturedAmong(IReadOnlySet<long> activeAirServiceIds)
            => AirServiceIds.Overlaps(activeAirServiceIds) && !AirServiceIds.IsSubsetOf(activeAirServiceIds);
    }
}
