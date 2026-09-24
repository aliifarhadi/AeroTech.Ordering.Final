namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IReservationDeadlineService
    {
        Task<IReadOnlyList<long>> ListDueOrderIdsAsync(int batchSize, CancellationToken cancellationToken = default);

        Task EnforceAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
