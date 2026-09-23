using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query._Shared.ReferenceCodes
{
    public sealed class ReferenceCodeReader : IReferenceCodeReader
    {
        private readonly OrderQueryDbContext _dbContext;

        public ReferenceCodeReader(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public async Task<ReferenceCodes> ReadAsync(ReferenceCodeKeys keys, CancellationToken cancellationToken = default)
        {
            var airlines = await ReadAsync(
                keys.AirlineIds,
                _dbContext.Airlines
                    .AsNoTracking()
                    .Where(airline => keys.AirlineIds.Contains(airline.Id))
                    .Select(airline => new CodeRow(airline.Id, airline.IataCode)),
                cancellationToken);

            var airports = await ReadAsync(
                keys.AirportIds,
                _dbContext.Airports
                    .AsNoTracking()
                    .Where(airport => keys.AirportIds.Contains(airport.Id))
                    .Select(airport => new CodeRow(airport.Id, airport.IataCode)),
                cancellationToken);

            var currencies = await ReadAsync(
                keys.CurrencyIds,
                _dbContext.Currencies
                    .AsNoTracking()
                    .Where(currency => keys.CurrencyIds.Contains(currency.Id))
                    .Select(currency => new CodeRow(currency.Id, currency.Code)),
                cancellationToken);

            var countries = await ReadAsync(
                keys.CountryIds,
                _dbContext.Countries
                    .AsNoTracking()
                    .Where(country => keys.CountryIds.Contains(country.Id))
                    .Select(country => new CodeRow(country.Id, country.Alpha2Code)),
                cancellationToken);

            return new ReferenceCodes(airlines, airports, currencies, countries, await ReadOfficesAsync(keys.Offices, cancellationToken));
        }

        private async Task<IReadOnlyDictionary<OfficeKey, OfficeCodes>> ReadOfficesAsync(
            IReadOnlyCollection<OfficeKey> offices,
            CancellationToken cancellationToken)
        {
            var resolved = new Dictionary<OfficeKey, OfficeCodes>();

            var airlineOfficeIds = IdsOf(offices, SellingOfficeKind.AirlineOffice);

            if (airlineOfficeIds.Count > 0)
                foreach (var office in await _dbContext.AirlineOffices
                             .AsNoTracking()
                             .Where(office => airlineOfficeIds.Contains(office.Id))
                             .Select(office => new OfficeRow(office.Id, office.Code, office.Name))
                             .ToListAsync(cancellationToken))
                    resolved[new OfficeKey(SellingOfficeKind.AirlineOffice, office.Id)] = new OfficeCodes(office.Code, office.Name);

            var travelAgencyOfficeIds = IdsOf(offices, SellingOfficeKind.TravelAgencyOffice);

            if (travelAgencyOfficeIds.Count > 0)
                foreach (var office in await _dbContext.TravelAgencyOffices
                             .AsNoTracking()
                             .Where(office => travelAgencyOfficeIds.Contains(office.Id))
                             .Select(office => new OfficeRow(office.Id, office.Code, office.Name))
                             .ToListAsync(cancellationToken))
                    resolved[new OfficeKey(SellingOfficeKind.TravelAgencyOffice, office.Id)] = new OfficeCodes(office.Code, office.Name);

            return resolved;
        }

        private static List<long> IdsOf(IReadOnlyCollection<OfficeKey> offices, SellingOfficeKind kind)
            => offices.Where(office => office.Kind == kind).Select(office => office.Id).Distinct().ToList();

        private static async Task<IReadOnlyDictionary<int, string>> ReadAsync(
            IReadOnlyCollection<int> ids,
            IQueryable<CodeRow> rows,
            CancellationToken cancellationToken)
            => ids.Count == 0
                ? new Dictionary<int, string>()
                : await rows.ToDictionaryAsync(row => row.Id, row => row.Code, cancellationToken);

        private sealed record CodeRow(int Id, string Code);

        private sealed record OfficeRow(long Id, string Code, string Name);
    }
}
