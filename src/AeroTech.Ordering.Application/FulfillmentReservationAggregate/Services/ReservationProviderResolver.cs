using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class ReservationProviderResolver : IReservationProviderResolver
    {
        private const string ReservationCapability = "Reservation";

        private readonly IReadOnlyDictionary<string, IReservationProvider> _providers;

        public ReservationProviderResolver(IEnumerable<IReservationProvider> providers)
            => _providers = providers.ToDictionary(provider => provider.ProviderKey, StringComparer.Ordinal);

        public IReservationProvider Resolve(string providerKey)
            => _providers.TryGetValue(providerKey, out var provider)
                ? provider
                : throw ExceptionFactory.NoFulfillmentAdapterRegistered(providerKey, ReservationCapability);

        public ReservationCapability CapabilityOf(OrderService service)
            => Resolve(service.FulfillmentProviderKey).CapabilityFor(service);

        public ReservationCapability CapabilityOf(Order order, FulfillmentReservation reservation)
        {
            var coveredServiceId = reservation.CoveredOrderServiceIds.First();

            return Resolve(reservation.FulfillmentProviderKey).CapabilityFor(order.Services.Single(service => service.Id == coveredServiceId));
        }

        public bool RequiresReservation(OrderService service)
            => service.CommercialStatus == OrderServiceCommercialState.Active
               && CapabilityOf(service).Mode != ReservationMode.None;
    }
}
