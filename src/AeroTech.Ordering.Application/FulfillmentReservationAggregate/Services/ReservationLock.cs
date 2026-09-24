using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Domain._Shared.Resources;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class ReservationLock : IReservationLock
    {
        private readonly IDistributedLock _distributedLock;
        private readonly TimeSpan _expiry;

        public ReservationLock(IDistributedLock distributedLock, IOptions<FulfillmentOptions> options)
        {
            _distributedLock = distributedLock;
            _expiry = TimeSpan.FromSeconds(options.Value.LockExpirySeconds);
        }

        public async Task<IAsyncDisposable> AcquireAsync(long orderId, CancellationToken cancellationToken = default)
            => await _distributedLock.AcquireAsync($"reservation:{orderId}", _expiry, cancellationToken)
               ?? throw ExceptionFactory.OrderOperationInProgress(orderId);
    }
}
