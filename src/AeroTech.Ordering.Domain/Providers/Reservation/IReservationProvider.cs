using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
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

        Task<ReservationValidationEvidence?> ValidateAsync(
            Order order,
            IReadOnlyCollection<long> orderServiceIds,
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

        ProviderRequest ConfirmRequestFor(ConfirmationIntent intent);

        Task<ConfirmationOutcome> ConfirmAsync(ProviderRequest request, CancellationToken cancellationToken = default);

        ProviderRequest CancelConfirmedRequestFor(ConfirmedCancellationIntent intent);

        Task<ConfirmedCancellationOutcome> CancelConfirmedAsync(ProviderRequest request, CancellationToken cancellationToken = default);
    }
}
