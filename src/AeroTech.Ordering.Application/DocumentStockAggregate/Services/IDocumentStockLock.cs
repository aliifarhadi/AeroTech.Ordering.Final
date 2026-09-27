using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Services
{
    public interface IDocumentStockLock
    {
        Task<IAsyncDisposable> AcquireAsync(long documentStockId, CancellationToken cancellationToken = default);

        Task<IAsyncDisposable> AcquireRangeAsync(
            int ownerAirlineId,
            AccountableDocumentKind documentKind,
            string prefix,
            CancellationToken cancellationToken = default);
    }
}
