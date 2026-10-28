using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public sealed class MigratedDatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString = TestDatabase.NewConnectionString();

    public TestDatabase For(IClock clock) => new(clock, _connectionString);

    public Task InitializeAsync() => For(new FixedClock()).MigrateAsync();

    public Task DisposeAsync() => For(new FixedClock()).DropAsync();
}
