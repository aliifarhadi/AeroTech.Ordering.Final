using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder
{
    public sealed record CancelOrderResult(
        long OrderId,
        OrderStatus OrderStatus,
        int CommercialVersion,
        long? OrderChangeId,
        IReadOnlyList<long> CancelledServiceIds,
        IReadOnlyList<CancellationReservationResult> Reservations);

    public sealed record CancellationReservationResult(
        long ReservationId,
        string FulfillmentProviderKey,
        FulfillmentReservationStatus Status,
        FulfillmentFailureReason? FailureReason,
        string? Error);
}
