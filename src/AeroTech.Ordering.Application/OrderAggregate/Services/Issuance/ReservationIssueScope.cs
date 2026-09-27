using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Issuance
{
    public sealed record ReservationIssueScope(
        FulfillmentReservation Reservation,
        IReadOnlyList<long> ReservationUnitIds,
        IReadOnlyList<long> OrderServiceIds);
}
