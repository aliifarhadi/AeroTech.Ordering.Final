using AeroTech.Framework.Core.ServiceContracts;

namespace AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts
{
    public interface IDocumentStockQueryDbSynchronizer : IQueryDbSynchronizer
    {
        Task ProjectAsync(DocumentStockReadModelSnapshot snapshot, CancellationToken cancellationToken = default);
    }
}
