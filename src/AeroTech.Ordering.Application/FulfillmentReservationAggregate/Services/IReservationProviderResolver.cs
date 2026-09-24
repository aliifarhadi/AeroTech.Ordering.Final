using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IReservationProviderResolver
    {
        IReservationProvider Resolve(string providerKey);

        ReservationCapability CapabilityOf(OrderService service);

        bool RequiresReservation(OrderService service);
    }
}
