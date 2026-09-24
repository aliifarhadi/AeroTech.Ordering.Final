namespace AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate.Requests
{
    public sealed record ReserveServicesRequest(IReadOnlyCollection<long> OrderServiceIds);
}
