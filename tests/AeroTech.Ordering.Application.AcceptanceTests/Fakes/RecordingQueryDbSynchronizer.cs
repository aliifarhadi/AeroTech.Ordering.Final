using AeroTech.Ordering.Domain.OrderAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class RecordingQueryDbSynchronizer : IOrderQueryDbSynchronizer
{
    public List<OrderReadModelSnapshot> ReservationProjections { get; } = [];

    public Task ProjectCreatedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ProjectRemarkedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ProjectReservationChangedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ReservationProjections.Add(snapshot);
        return Task.CompletedTask;
    }
}
