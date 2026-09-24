using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record FlightReservationUnitDetails(
        long FlightId,
        long FlightCapacityId,
        string PaxReference,
        string? RequestedSeat) : ReservationUnitDetails;
}
