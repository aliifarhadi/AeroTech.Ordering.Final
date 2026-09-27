using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts
{
    public interface IFulfillmentTaskRepository
    {
        Task AddAsync(FulfillmentTask task, CancellationToken cancellationToken = default);

        Task<FulfillmentTask?> FindLatestAsync(
            long fulfillmentReservationId,
            OrderFulfillmentTaskType taskType,
            CancellationToken cancellationToken = default);

        Task<bool> AnyUnresolvedAsync(
            long orderId,
            IReadOnlyCollection<long> fulfillmentReservationIds,
            IReadOnlyCollection<OrderFulfillmentTaskType> taskTypes,
            CancellationToken cancellationToken = default);
    }
}
