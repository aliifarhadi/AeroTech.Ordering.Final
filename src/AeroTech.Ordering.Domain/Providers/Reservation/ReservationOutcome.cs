using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.Providers.Reservation
{
    public sealed record ReservationOutcome(
        ProviderOperationOutcome OperationOutcome,
        string? ProviderOperationRef,
        string? EchoedIdempotencyKey,
        string? EchoedCorrelationReference,
        DateTimeOffset? ExpiresAt,
        IReadOnlyList<ReservationUnitOutcome> Units,
        ProviderFailure? Failure);

    public sealed record ReservationUnitOutcome(
        string UnitCorrelationKey,
        IReadOnlyList<long> OrderServiceIds,
        ReservationMemberStatus Status,
        string? ProviderUnitRef,
        string? RawStatusCode,
        string? ObservedSeat);

    public sealed record ReleaseOutcome(
        ProviderOperationOutcome OperationOutcome,
        ProviderFailure? Failure);

    public sealed record ProviderFailure(
        FulfillmentFailureKind Kind,
        FulfillmentFailureReason Reason,
        string Message,
        int? ProviderStatusCode);
}
