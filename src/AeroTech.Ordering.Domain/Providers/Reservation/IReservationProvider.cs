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

        ProviderRequest ReserveRequestFor(Order order, ReservationIntent intent);

        Task<ReservationOutcome> ReserveAsync(
            ReservationIntent intent,
            ProviderRequest request,
            CancellationToken cancellationToken = default);

        ProviderRequest ReadRequestFor(string providerOperationRef);

        Task<ReservationOutcome> ReadAsync(
            ReservationIntent intent,
            ProviderRequest request,
            CancellationToken cancellationToken = default);

        ProviderRequest ReleaseRequestFor(ReleaseIntent intent);

        Task<ReleaseOutcome> ReleaseAsync(ProviderRequest request, CancellationToken cancellationToken = default);
    }
}
