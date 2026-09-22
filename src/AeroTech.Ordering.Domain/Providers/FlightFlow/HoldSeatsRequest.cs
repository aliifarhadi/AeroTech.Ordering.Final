using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;
using PassengerTypeCode = AeroTech.Messages.AirPrice.Enums.PassengerTypeCode;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record HoldSeatsRequest(
        string IdempotencyKey,
        string Reference,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<PassengerForHoldSeatRequest> Passengers,
        IReadOnlyList<FlightForHoldSeatRequest> Flights);

    public sealed record PassengerForHoldSeatRequest(
        string PaxReference,
        PassengerTypeCode Type,
        Gender Gender);

    public sealed record FlightForHoldSeatRequest(
        string FlightCapId,
        IReadOnlyList<SeatForHoldSeatRequest> Seats);

    public sealed record SeatForHoldSeatRequest(
        string PaxReference,
        decimal Revenue,
        string? Seat);
}
