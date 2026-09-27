using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record ConfirmHoldResult(FulfillmentReservationStatus HoldStatus, string? Reason);
}
