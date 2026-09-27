using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate;

namespace AeroTech.Ordering.Domain.Providers.Pricing
{
    public interface IAirFareReservationValidator
    {
        Task<ReservationValidationEvidence> ValidateAsync(
            Order order,
            IReadOnlyCollection<long> airServiceIds,
            CancellationToken cancellationToken = default);
    }
}
