using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record CancelConfirmedSeatsResult(
        string? HoldBatchId,
        IReadOnlyList<CancelledConfirmedSeat>? Seats,
        FulfillmentReservationStatus? RefusedStatus,
        string? Reason);

    public sealed record CancelledConfirmedSeat(
        string? SeatHoldReference,
        FlightSeatHoldStatus? Status);
}
