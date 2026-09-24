using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<long, Order> _orders = [];

    public void Seed(Order order) => _orders[order.Id] = order;

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        Seed(order);
        return Task.CompletedTask;
    }

    public Task<Order?> GetAsync(long id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orders.GetValueOrDefault(id));

    public Task<Order?> GetByReferenceAsync(Guid orderReference, CancellationToken cancellationToken = default)
        => Task.FromResult(_orders.Values.FirstOrDefault(order => order.OrderReference == orderReference));

    public Task<bool> RecordLocatorExistsAsync(string recordLocator, CancellationToken cancellationToken = default)
        => Task.FromResult(_orders.Values.Any(order => order.RecordLocator == recordLocator));

    public Task<IReadOnlyList<long>> ListReservableIdsPastLastTicketingDateAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<long>>(_orders.Values
            .Where(order => order.LastTicketingDate <= now && Order.ReservableStatuses.Contains(order.Status))
            .OrderBy(order => order.LastTicketingDate)
            .Select(order => order.Id)
            .Take(batchSize)
            .ToList());
}
