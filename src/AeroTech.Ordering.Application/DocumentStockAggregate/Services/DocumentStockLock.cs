using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Domain._Shared.Resources;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Services
{
    public sealed class DocumentStockLock : IDocumentStockLock
    {
        private readonly IDistributedLock _distributedLock;
        private readonly TimeSpan _expiry;

        public DocumentStockLock(IDistributedLock distributedLock, IOptions<FulfillmentOptions> options)
        {
            _distributedLock = distributedLock;
            _expiry = TimeSpan.FromSeconds(options.Value.LockExpirySeconds);
        }

        public async Task<IAsyncDisposable> AcquireAsync(long documentStockId, CancellationToken cancellationToken = default)
            => await _distributedLock.AcquireAsync($"document-stock:{documentStockId}", _expiry, cancellationToken)
               ?? throw ExceptionFactory.DocumentStockOperationInProgress(documentStockId);

        public async Task<IAsyncDisposable> AcquireRangeAsync(
            int ownerAirlineId,
            AccountableDocumentKind documentKind,
            string prefix,
            CancellationToken cancellationToken = default)
            => await _distributedLock.AcquireAsync($"document-stock-range:{ownerAirlineId}:{documentKind}:{prefix}", _expiry, cancellationToken)
               ?? throw ExceptionFactory.DocumentStockOperationInProgress(prefix);
    }
}
