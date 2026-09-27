using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubAirlineOfficeTimeZoneResolver : IAirlineOfficeTimeZoneResolver
{
    public Dictionary<long, string?> TimeZones { get; } = [];

    public Task<string?> FindTimeZoneIdAsync(long airlineOfficeId, CancellationToken cancellationToken = default)
        => Task.FromResult(TimeZones.GetValueOrDefault(airlineOfficeId));
}
