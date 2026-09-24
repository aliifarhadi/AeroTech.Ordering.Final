using AeroTech.Ordering.Application._Shared.Authorization;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public interface IReserveService
    {
        Task<ReserveResult> ReserveOrderAsync(
            long orderId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);

        Task<ReserveResult> ReserveServicesAsync(
            long orderId,
            IReadOnlyCollection<long> orderServiceIds,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);
    }
}
