using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class TaxJurisdictionSnapshot : ValueObject
    {
        private TaxJurisdictionSnapshot()
        {
        }

        private TaxJurisdictionSnapshot(int? countryId, int? stationId)
        {
            CountryId = countryId;
            StationId = stationId;
        }

        public int? CountryId { get; private set; }

        public int? StationId { get; private set; }

        public static TaxJurisdictionSnapshot? Of(int? countryId, int? stationId)
            => countryId is null && stationId is null ? null : new TaxJurisdictionSnapshot(countryId, stationId);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return CountryId;
            yield return StationId;
        }
    }
}
