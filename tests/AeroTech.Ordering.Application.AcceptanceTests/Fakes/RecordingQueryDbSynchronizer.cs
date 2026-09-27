using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class RecordingQueryDbSynchronizer : IOrderQueryDbSynchronizer, IElectronicTicketQueryDbSynchronizer, IDocumentStockQueryDbSynchronizer
{
    public List<OrderReadModelSnapshot> ReservationProjections { get; } = [];

    public List<OrderReadModelSnapshot> IssueProjections { get; } = [];

    public List<ElectronicTicketReadModelSnapshot> TicketProjections { get; } = [];

    public List<DocumentStockReadModelSnapshot> StockProjections { get; } = [];

    public Task ProjectCreatedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ProjectRemarkedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ProjectReservationChangedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ReservationProjections.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task ProjectIssuedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        IssueProjections.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task ProjectIssuedAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken = default)
    {
        TicketProjections.AddRange(tickets);
        return Task.CompletedTask;
    }

    public Task ProjectAsync(DocumentStockReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        StockProjections.Add(snapshot);
        return Task.CompletedTask;
    }
}
