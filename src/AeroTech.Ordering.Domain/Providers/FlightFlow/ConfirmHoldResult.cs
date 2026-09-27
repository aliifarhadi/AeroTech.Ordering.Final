using AeroTech.Messages.FlightFlow.Enums;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record ConfirmHoldResult(FlightSeatHoldStatus? HoldStatus, string? Reason);
}
