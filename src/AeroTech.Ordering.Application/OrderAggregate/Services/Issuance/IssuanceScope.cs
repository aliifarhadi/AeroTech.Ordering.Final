using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Issuance
{
    public sealed record IssuanceScope(
        IReadOnlyList<OrderAirTransportService> Services,
        IReadOnlyList<FulfillmentReservation> Reservations);
}
