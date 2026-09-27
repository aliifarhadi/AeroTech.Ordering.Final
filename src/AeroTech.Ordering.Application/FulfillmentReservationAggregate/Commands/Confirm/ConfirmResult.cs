using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public sealed record ConfirmResult(
        long OrderId,
        OrderStatus Status,
        IReadOnlyList<ReservationConfirmationResult> Reservations);

    public sealed record ReservationConfirmationResult(
        long ReservationId,
        string FulfillmentProviderKey,
        FulfillmentReservationStatus Status,
        FulfillmentFailureReason? FailureReason,
        string? Error);
}
