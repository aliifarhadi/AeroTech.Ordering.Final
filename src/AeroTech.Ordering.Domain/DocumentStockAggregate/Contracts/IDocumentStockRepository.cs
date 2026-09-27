using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts
{
    public interface IDocumentStockRepository
    {
        Task AddAsync(DocumentStock stock, CancellationToken cancellationToken = default);

        Task<DocumentStock?> GetAsync(long id, CancellationToken cancellationToken = default);

        Task ReloadAsync(DocumentStock stock, CancellationToken cancellationToken = default);

        Task<long?> FindOverlappingIdAsync(
            int ownerAirlineId,
            AccountableDocumentKind documentKind,
            string prefix,
            long rangeFrom,
            long rangeTo,
            CancellationToken cancellationToken = default);
    }
}
