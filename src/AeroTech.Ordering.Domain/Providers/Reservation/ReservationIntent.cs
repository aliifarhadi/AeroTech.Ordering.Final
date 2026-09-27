using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.Providers.Reservation
{
    public sealed record ReservationIntent(
        string ProviderKey,
        string IdempotencyKey,
        string CorrelationReference,
        DateTimeOffset? RequestedExpiresAt,
        IReadOnlyList<ReservationUnitIntent> Units);

    public sealed record ReservationUnitIntent(
        string UnitCorrelationKey,
        IReadOnlyList<long> OrderServiceIds,
        ReservationUnitDetails Details);

    public abstract record ReservationUnitDetails;

    public sealed record ReservationPreparation(
        DateTimeOffset? RequestedExpiresAt,
        ReservationValidationEvidence? ValidationEvidence);

    public sealed record ReleaseIntent(
        string ProviderKey,
        string ProviderOperationRef,
        string IdempotencyKey);

    public sealed record ConfirmationIntent(
        string ProviderKey,
        string ProviderOperationRef);

    public sealed record ConfirmedCancellationIntent(
        string ProviderKey,
        string ProviderOperationRef,
        IReadOnlyList<ConfirmedCancellationUnit> Units,
        string CommercialReason);

    public sealed record ConfirmedCancellationUnit(
        long ReservationUnitId,
        string ProviderUnitRef);

    public sealed record ProviderRequest(
        ProviderInteractionType InteractionType,
        string? IdempotencyKey,
        string? CorrelationReference,
        string Payload);
}
