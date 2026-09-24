using AeroTech.Ordering.Domain.OrderAggregate;

namespace AeroTech.Ordering.Domain.Providers.Pricing
{
    public interface IAirFareReservationValidator
    {
        Task<DateTimeOffset> ValidateAsync(
            Order order,
            IReadOnlyCollection<long> airServiceIds,
            CancellationToken cancellationToken = default);
    }
}
