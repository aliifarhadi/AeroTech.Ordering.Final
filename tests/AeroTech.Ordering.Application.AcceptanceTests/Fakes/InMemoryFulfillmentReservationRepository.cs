using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryFulfillmentReservationRepository(
    InMemoryUnitOfWork unitOfWork,
    InMemoryOrderRepository orders,
    InMemoryFulfillmentTaskRepository tasks) : IFulfillmentReservationRepository
{
    private readonly List<FulfillmentReservation> _committed = [];

    public IReadOnlyList<FulfillmentReservation> Committed => _committed;

    public Task AddAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default)
    {
        unitOfWork.Stage(() => _committed.Add(reservation));
        return Task.CompletedTask;
    }

    public Task<FulfillmentReservation?> GetAsync(long id, CancellationToken cancellationToken = default)
        => Task.FromResult(_committed.FirstOrDefault(reservation => reservation.Id == id));

    public Task<IReadOnlyList<FulfillmentReservation>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<FulfillmentReservation>>(_committed.Where(reservation => reservation.OrderId == orderId).ToList());

    public Task<IReadOnlyList<long>> ListOrderIdsWithDueHoldsAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<long>>(_committed
            .Where(reservation => reservation.Status == FulfillmentReservationStatus.Held)
            .Where(reservation => reservation.HoldLapsedAt(now)
                                  || (HasPassedLastTicketingDate(reservation, now) && !ReleaseWasRejected(reservation)))
            .Select(reservation => reservation.OrderId)
            .Distinct()
            .Take(batchSize)
            .ToList());

    private bool HasPassedLastTicketingDate(FulfillmentReservation reservation, DateTimeOffset now)
        => orders.Find(reservation.OrderId)?.HasPassedLastTicketingDateAt(now) == true;

    private bool ReleaseWasRejected(FulfillmentReservation reservation)
        => tasks.Latest(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved) is { Status: OrderFulfillmentStatus.Failed };
}
