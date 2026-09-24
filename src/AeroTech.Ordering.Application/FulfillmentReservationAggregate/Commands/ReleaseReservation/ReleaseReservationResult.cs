using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public sealed record ReleaseReservationResult(
        long OrderId,
        OrderStatus Status,
        long ReservationId,
        FulfillmentReservationStatus ReservationStatus,
        FulfillmentFailureReason? FailureReason,
        string? Error);
}
