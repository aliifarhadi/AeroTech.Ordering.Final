using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.ReferenceData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.ServiceHost.ReferenceData
{
    public sealed class ReferenceDataCountryCodeResolver : ICountryCodeResolver
    {
        private readonly ReferenceDbContext _referenceData;

        public ReferenceDataCountryCodeResolver(ReferenceDbContext referenceData) => _referenceData = referenceData;

        public async Task<int> ResolveAsync(string alpha2Code, CancellationToken cancellationToken = default)
            => await ResolveOptionalAsync(alpha2Code, cancellationToken)
               ?? throw ExceptionFactory.CountryCodeIsNotRecognised(alpha2Code);

        public async Task<int?> ResolveOptionalAsync(string? alpha2Code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(alpha2Code))
                return null;

            var normalised = alpha2Code.Trim().ToUpperInvariant();

            return await _referenceData.Countries
                .AsNoTracking()
                .Where(country => country.Alpha2Code == normalised)
                .Select(country => (int?)country.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
