using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryDocumentStockRepository(InMemoryUnitOfWork unitOfWork) : IDocumentStockRepository
{
    private readonly List<DocumentStock> _committed = [];

    public IReadOnlyList<DocumentStock> Committed => _committed;

    public Action<DocumentStock>? OnReload { get; set; }

    public Task AddAsync(DocumentStock stock, CancellationToken cancellationToken = default)
    {
        unitOfWork.Stage(() => _committed.Add(stock));
        return Task.CompletedTask;
    }

    public Task<DocumentStock?> GetAsync(long id, CancellationToken cancellationToken = default)
        => Task.FromResult(_committed.FirstOrDefault(stock => stock.Id == id));

    public Task ReloadAsync(DocumentStock stock, CancellationToken cancellationToken = default)
    {
        OnReload?.Invoke(stock);
        return Task.CompletedTask;
    }

    public Task<long?> FindOverlappingIdAsync(
        int ownerAirlineId,
        AccountableDocumentKind documentKind,
        string prefix,
        long rangeFrom,
        long rangeTo,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_committed
            .Where(stock => stock.OwnerAirlineId == ownerAirlineId
                            && stock.DocumentKind == documentKind
                            && stock.Prefix == prefix
                            && stock.Overlaps(rangeFrom, rangeTo))
            .Select(stock => (long?)stock.Id)
            .FirstOrDefault());
}
