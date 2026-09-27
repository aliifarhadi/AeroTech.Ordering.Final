using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation
{
    public abstract record ReservationSettlement(FulfillmentReservation Reservation, IReadOnlyList<long> UnitIds);

    public sealed record HeldRelease(FulfillmentReservation Reservation, IReadOnlyList<long> UnitIds)
        : ReservationSettlement(Reservation, UnitIds);

    public sealed record ConfirmedCancellation(FulfillmentReservation Reservation, IReadOnlyList<long> UnitIds)
        : ReservationSettlement(Reservation, UnitIds);

    public sealed record CancellationRecovery(FulfillmentReservation Reservation, IReadOnlyList<long> UnitIds)
        : ReservationSettlement(Reservation, UnitIds);
}
