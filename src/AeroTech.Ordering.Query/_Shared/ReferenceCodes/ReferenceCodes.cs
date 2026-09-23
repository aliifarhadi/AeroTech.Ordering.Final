using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query._Shared.ReferenceCodes
{
    public readonly record struct OfficeKey(SellingOfficeKind Kind, long Id);

    public sealed record OfficeCodes(string Code, string Name);

    public sealed record ReferenceCodeKeys(
        IReadOnlyCollection<int> AirlineIds,
        IReadOnlyCollection<int> AirportIds,
        IReadOnlyCollection<int> CurrencyIds,
        IReadOnlyCollection<int> CountryIds,
        IReadOnlyCollection<OfficeKey> Offices);

    public sealed class ReferenceCodes
    {
        public ReferenceCodes(
            IReadOnlyDictionary<int, string> airlines,
            IReadOnlyDictionary<int, string> airports,
            IReadOnlyDictionary<int, string> currencies,
            IReadOnlyDictionary<int, string> countries,
            IReadOnlyDictionary<OfficeKey, OfficeCodes> offices)
        {
            _airlines = airlines;
            _airports = airports;
            _currencies = currencies;
            _countries = countries;
            _offices = offices;
        }

        private readonly IReadOnlyDictionary<int, string> _airlines;
        private readonly IReadOnlyDictionary<int, string> _airports;
        private readonly IReadOnlyDictionary<int, string> _currencies;
        private readonly IReadOnlyDictionary<int, string> _countries;
        private readonly IReadOnlyDictionary<OfficeKey, OfficeCodes> _offices;

        public string? Airline(int id) => _airlines.GetValueOrDefault(id);

        public string? Airport(int id) => _airports.GetValueOrDefault(id);

        public string? Currency(int id) => _currencies.GetValueOrDefault(id);

        public string? Country(int? id) => id is null ? null : _countries.GetValueOrDefault(id.Value);

        public OfficeCodes? Office(SellingOfficeKind kind, long id) => _offices.GetValueOrDefault(new OfficeKey(kind, id));
    }
}
