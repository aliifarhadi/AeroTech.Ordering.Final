using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IReservationProviderResolver
    {
        IReservationProvider Resolve(string providerKey);

        ReservationCapability CapabilityOf(OrderService service);

        ReservationCapability CapabilityOf(Order order, FulfillmentReservation reservation);

        bool RequiresReservation(OrderService service);
    }
}
