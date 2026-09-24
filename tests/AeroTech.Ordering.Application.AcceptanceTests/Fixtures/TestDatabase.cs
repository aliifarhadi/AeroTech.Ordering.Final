using AeroTech.Framework.Core.Domain.Events;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public sealed class TestDatabase(IClock clock) : IAsyncDisposable
{
    private readonly string _connectionString = new SqlConnectionStringBuilder(DevelopmentConnectionString())
    {
        InitialCatalog = $"DotAirOrdering_Tests_{Guid.NewGuid():N}"
    }.ConnectionString;

    public OrderingDbContext NewContext()
        => new(
            new DbContextOptionsBuilder<OrderingDbContext>()
                .UseSqlServer(_connectionString, sql => sql.MigrationsHistoryTable(OrderingDbContext.MigrationsHistoryTable, OrderingDbContext.MigrationsHistorySchema))
                .Options,
            new AnonymousActorResolver(),
            clock,
            new IgnoringDomainEventDispatcher());

    public async ValueTask DisposeAsync()
    {
        await using var context = NewContext();
        await context.Database.EnsureDeletedAsync();
    }

    private static string DevelopmentConnectionString()
        => new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(RepositoryRoot(), "src", "AeroTech.Ordering.ServiceHost", "appsettings.Development.json"))
               .Build()
               .GetConnectionString("CommandDbContext")
           ?? throw new InvalidOperationException("ConnectionStrings:CommandDbContext is not configured.");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AeroTech.Ordering.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("AeroTech.Ordering.sln was not found above the test output.");
    }

    private sealed class AnonymousActorResolver : IActorResolver
    {
        public Actor Resolve() => Actor.Anonymous;
    }

    private sealed class IgnoringDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
