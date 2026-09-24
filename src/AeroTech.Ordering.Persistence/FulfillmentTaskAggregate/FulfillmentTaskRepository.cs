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
    }
}
