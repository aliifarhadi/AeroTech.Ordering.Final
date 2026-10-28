using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class MigratedDatabaseCollection : ICollectionFixture<MigratedDatabaseFixture>
{
    public const string Name = "Migrated database";
}
