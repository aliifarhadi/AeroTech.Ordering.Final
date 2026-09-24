using AeroTech.Messages.Ordering.Enums;

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
        DateTimeOffset? ValidationTimeLimit);

    public sealed record ReleaseIntent(
        string ProviderKey,
        string ProviderOperationRef,
        string IdempotencyKey);

    public sealed record ProviderRequest(
        ProviderInteractionType InteractionType,
        string? IdempotencyKey,
        string? CorrelationReference,
        string Payload);
}
