using AeroTech.Framework.Core.Domain.Repository;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _pending = [];

    public int SaveCount { get; private set; }

    public void Stage(Action commit) => _pending.Add(commit);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var committed = _pending.Count;

        foreach (var commit in _pending)
            commit();

        _pending.Clear();
        SaveCount++;

        return Task.FromResult(committed);
    }
}
