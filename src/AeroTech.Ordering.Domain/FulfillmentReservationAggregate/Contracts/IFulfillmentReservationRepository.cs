namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts
{
    public interface IFulfillmentReservationRepository
    {
        Task AddAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default);

        Task<FulfillmentReservation?> GetAsync(long id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FulfillmentReservation>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
