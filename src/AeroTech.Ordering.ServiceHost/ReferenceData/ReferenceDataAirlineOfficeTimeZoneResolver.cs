using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.ReferenceData.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.ServiceHost.ReferenceData
{
    public sealed class ReferenceDataAirlineOfficeTimeZoneResolver : IAirlineOfficeTimeZoneResolver
    {
        private readonly ReferenceDbContext _referenceData;

        public ReferenceDataAirlineOfficeTimeZoneResolver(ReferenceDbContext referenceData) => _referenceData = referenceData;

        public Task<string?> FindTimeZoneIdAsync(long airlineOfficeId, CancellationToken cancellationToken = default)
            => _referenceData.AirlineOffices
                .AsNoTracking()
                .Where(office => office.Id == airlineOfficeId)
                .Select(office => office.TimeZoneId)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
