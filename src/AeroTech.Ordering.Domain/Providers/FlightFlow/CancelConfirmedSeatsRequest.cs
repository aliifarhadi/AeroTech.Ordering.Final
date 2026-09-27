using AeroTech.Messages.FlightFlow.Enums;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record CancelConfirmedSeatsRequest(
        string HoldBatchId,
        IReadOnlyList<string> SeatHoldReferences,
        FlightSeatHoldCancellationReason ReasonCode);
}
