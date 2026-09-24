using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IReservationReleaser
    {
        Task<ReleaseOutcome> ReleaseAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default);

        Task<bool> ReleaseWasRejectedAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default);
    }
}
