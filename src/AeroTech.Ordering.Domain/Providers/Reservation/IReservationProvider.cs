using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;

namespace AeroTech.Ordering.Domain.Providers.Reservation
{
    public interface IReservationProvider
    {
        string ProviderKey { get; }

        ReservationCapability CapabilityFor(OrderService service);

        IReadOnlyList<ReservationUnitIntent> PlanUnits(Order order, IReadOnlyCollection<OrderService> services);

        Task<ReservationPreparation> PrepareAsync(
            Order order,
            IReadOnlyList<ReservationUnitIntent> units,
            CancellationToken cancellationToken = default);

        Task<ReservationOutcome> ReserveAsync(
            Order order,
            ReservationIntent intent,
            CancellationToken cancellationToken = default);

        Task<ReleaseOutcome> ReleaseAsync(ReleaseIntent intent, CancellationToken cancellationToken = default);
    }
}
