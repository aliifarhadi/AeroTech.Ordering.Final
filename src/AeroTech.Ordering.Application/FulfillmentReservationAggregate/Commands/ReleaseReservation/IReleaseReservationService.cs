using AeroTech.Ordering.Application._Shared.Authorization;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public interface IReleaseReservationService
    {
        Task<ReleaseReservationResult> ReleaseAsync(
            long orderId,
            long reservationId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);
    }
}
