namespace AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate.Requests
{
    public sealed record ConfirmReservationsRequest(IReadOnlyCollection<long>? ReservationIds);
}
