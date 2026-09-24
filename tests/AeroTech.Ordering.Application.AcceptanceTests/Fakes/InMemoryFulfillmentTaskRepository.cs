using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryFulfillmentTaskRepository(InMemoryUnitOfWork unitOfWork) : IFulfillmentTaskRepository
{
    private readonly List<FulfillmentTask> _committed = [];

    public IReadOnlyList<FulfillmentTask> Committed => _committed;

    public Task AddAsync(FulfillmentTask task, CancellationToken cancellationToken = default)
    {
        unitOfWork.Stage(() => _committed.Add(task));
        return Task.CompletedTask;
    }

    public Task<FulfillmentTask?> FindLatestAsync(
        long fulfillmentReservationId,
        OrderFulfillmentTaskType taskType,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_committed
            .Where(task => task.FulfillmentReservationId == fulfillmentReservationId && task.TaskType == taskType)
            .OrderByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id)
            .FirstOrDefault());
}
