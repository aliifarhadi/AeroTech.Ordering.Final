using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Persistence.FulfillmentTaskAggregate
{
    public sealed class FulfillmentTaskRepository : IFulfillmentTaskRepository
    {
        private readonly OrderingDbContext _dbContext;

        public FulfillmentTaskRepository(OrderingDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(FulfillmentTask task, CancellationToken cancellationToken = default)
            => await _dbContext.FulfillmentTasks.AddAsync(task, cancellationToken);

        public Task<FulfillmentTask?> FindLatestAsync(
            long fulfillmentReservationId,
            OrderFulfillmentTaskType taskType,
            CancellationToken cancellationToken = default)
            => _dbContext.FulfillmentTasks
                .Include(task => task.Targets)
                .Include(task => task.Attempts)
                .Include(task => task.Interactions)
                .AsSplitQuery()
                .Where(task => task.FulfillmentReservationId == fulfillmentReservationId && task.TaskType == taskType)
                .OrderByDescending(task => task.CreatedAt)
                .ThenByDescending(task => task.Id)
                .FirstOrDefaultAsync(cancellationToken);

        public Task<bool> AnyUnresolvedAsync(
            long orderId,
            IReadOnlyCollection<long> fulfillmentReservationIds,
            IReadOnlyCollection<OrderFulfillmentTaskType> taskTypes,
            CancellationToken cancellationToken = default)
            => _dbContext.FulfillmentTasks
                .Where(task => task.OrderId == orderId && taskTypes.Contains(task.TaskType))
                .Where(task => task.Status == OrderFulfillmentStatus.Pending
                               || task.Status == OrderFulfillmentStatus.InProgress
                               || task.Status == OrderFulfillmentStatus.Unknown)
                .Where(task => task.FulfillmentReservationId == null
                               || fulfillmentReservationIds.Contains(task.FulfillmentReservationId.Value))
                .AnyAsync(cancellationToken);
    }
}
