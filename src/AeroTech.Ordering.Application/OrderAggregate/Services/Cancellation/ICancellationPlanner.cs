using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation
{
    public interface ICancellationPlanner
    {
        void EnsureNoActiveDocumentCovers(IReadOnlyCollection<OrderService> endingServices, IReadOnlyCollection<ElectronicTicket> tickets);

        IReadOnlyList<ReservationSettlement> PlanSettlements(
            Order order,
            IReadOnlyCollection<OrderService> endingServices,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlySet<long> reservationsWithResumableCancellation);

        bool IsSettled(
            Order order,
            IReadOnlyCollection<OrderService> endingServices,
            IReadOnlyCollection<FulfillmentReservation> reservations);
    }
}
