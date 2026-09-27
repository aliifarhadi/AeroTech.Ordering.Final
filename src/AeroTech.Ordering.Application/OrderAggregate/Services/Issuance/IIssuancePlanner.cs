using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Issuance
{
    public interface IIssuancePlanner
    {
        IReadOnlyList<OrderAirTransportService> OutstandingServices(Order order, IReadOnlyCollection<ElectronicTicket> tickets);

        IssuanceScope ScopeOf(
            IReadOnlyList<OrderAirTransportService> services,
            IReadOnlyCollection<FulfillmentReservation> reservations);

        IReadOnlyList<TicketPlan> PlanTickets(Order order, IssuanceScope scope);
    }
}
