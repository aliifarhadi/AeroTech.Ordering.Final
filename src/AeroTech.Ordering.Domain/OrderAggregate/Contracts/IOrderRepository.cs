namespace AeroTech.Ordering.Domain.OrderAggregate.Contracts
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken cancellationToken = default);

        Task<Order?> GetAsync(long id, CancellationToken cancellationToken = default);

        Task<Order?> GetByReferenceAsync(Guid orderReference, CancellationToken cancellationToken = default);

        Task<bool> RecordLocatorExistsAsync(string recordLocator, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<long>> ListReservableIdsPastLastTicketingDateAsync(
            DateTimeOffset now,
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}
