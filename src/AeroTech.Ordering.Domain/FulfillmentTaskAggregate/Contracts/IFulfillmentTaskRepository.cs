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
    }
}
