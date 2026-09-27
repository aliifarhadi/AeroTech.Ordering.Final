using AeroTech.Ordering.Application._Shared.Authorization;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public interface IConfirmService
    {
        Task<ConfirmResult> ConfirmReservedCapacityAsync(
            long orderId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);

        Task<ConfirmResult> ConfirmReservationsAsync(
            long orderId,
            IReadOnlyCollection<long> reservationIds,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);
    }
}
