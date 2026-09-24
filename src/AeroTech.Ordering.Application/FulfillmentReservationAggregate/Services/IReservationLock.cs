namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IReservationLock
    {
        Task<IAsyncDisposable> AcquireAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
