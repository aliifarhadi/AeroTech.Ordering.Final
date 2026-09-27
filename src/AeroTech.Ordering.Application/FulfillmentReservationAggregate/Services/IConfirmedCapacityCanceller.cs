using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IConfirmedCapacityCanceller
    {
        Task<IReadOnlyDictionary<long, IReadOnlySet<long>>> UnresolvedTargetsAsync(
            IReadOnlyCollection<FulfillmentReservation> reservations,
            CancellationToken cancellationToken = default);

        Task<ConfirmedCancellationOutcome?> ResumeAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default);

        Task<ConfirmedCancellationOutcome> CancelAsync(
            FulfillmentReservation reservation,
            IReadOnlyCollection<long> unitIds,
            string commercialReason,
            CancellationToken cancellationToken = default);
    }
}
