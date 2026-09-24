using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryFulfillmentReservationRepository(InMemoryUnitOfWork unitOfWork) : IFulfillmentReservationRepository
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
            .Where(reservation => reservation.Status == FulfillmentReservationStatus.Held
                                  && (reservation.HoldLapsedAt(now) || reservation.ValidationTimeLimitPassedAt(now)))
            .Select(reservation => reservation.OrderId)
            .Distinct()
            .Take(batchSize)
            .ToList());
}
