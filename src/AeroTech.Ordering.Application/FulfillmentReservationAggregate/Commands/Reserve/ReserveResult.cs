using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public sealed record ReserveResult(
        long OrderId,
        OrderStatus Status,
        string? RecordLocator,
        IReadOnlyList<ReservationOperationResult> Reservations);

    public sealed record ReservationOperationResult(
        long ReservationId,
        string FulfillmentProviderKey,
        FulfillmentReservationStatus Status,
        string? ProviderOperationRef,
        DateTimeOffset? ExpiresAt,
        FulfillmentFailureReason? FailureReason,
        string? Error);
}
