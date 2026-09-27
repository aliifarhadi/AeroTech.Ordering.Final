using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        public IReadOnlyList<OrderService> CancellationScopeOf(IReadOnlyCollection<long> requestedServiceIds)
        {
            var requested = requestedServiceIds.Count == 0
                ? _services.Where(service => service.IsActive).ToList()
                : requestedServiceIds.Distinct().Select(RequestedService).Where(service => service.IsActive).ToList();

            var airServices = requested
                .OfType<OrderAirTransportService>()
                .SelectMany(airService => DependentInfantAirServicesOf(airService).Prepend(airService))
                .DistinctBy(airService => airService.Id)
                .ToList();

            var airServiceIds = airServices.Select(airService => airService.Id).ToHashSet();

            if (requested.OfType<OrderSeatService>().FirstOrDefault(seat => !airServiceIds.Contains(seat.AssociatedAirServiceId)) is { } seatOnly)
                throw ExceptionFactory.SeatCancellationRequiresAncillaryStage(seatOnly.Id);

            var seats = _services
                .OfType<OrderSeatService>()
                .Where(seat => seat.IsActive && airServiceIds.Contains(seat.AssociatedAirServiceId));

            return airServices.Concat<OrderService>(seats).ToList();
        }

        public OrderChange Cancel(
            IReadOnlyCollection<long> serviceIds,
            SalesContext actorContext,
            string reasonCode,
            string? reasonText,
            IIdGenerator idGenerator,
            DateTimeOffset committedAt)
        {
            var services = serviceIds.Distinct().Select(RequestedService).ToList();

            if (services.Count == 0)
                throw ExceptionFactory.CancellationRequiresServices(Id);

            if (services.FirstOrDefault(service => !service.IsActive) is { } inactive)
                throw ExceptionFactory.OrderServiceIsNotActive(inactive.Id, inactive.CommercialStatus);

            var change = new OrderChange(
                idGenerator.NewId(),
                Id,
                OrderChangeType.Cancel,
                CommercialVersion + 1,
                actorContext,
                null,
                null,
                reasonCode,
                reasonText,
                false,
                null,
                committedAt);

            CommercialVersion = change.CommercialVersion;
            _changes.Add(change);

            foreach (var service in services)
                service.Cancel(change.Id);

            foreach (var item in _items.Where(item => item.CommercialStatus == OrderItemCommercialState.Active && !HasActiveService(item)))
                item.Cancel(change.Id);

            return change;
        }

        private OrderService RequestedService(long serviceId)
        {
            var service = _services.FirstOrDefault(item => item.Id == serviceId)
                          ?? throw ExceptionFactory.OrderServiceIsNotInOrder(serviceId, Id);

            if (service.CommercialStatus is not (OrderServiceCommercialState.Active or OrderServiceCommercialState.Cancelled))
                throw ExceptionFactory.OrderServiceIsNotActive(service.Id, service.CommercialStatus);

            return service;
        }

        private IEnumerable<OrderAirTransportService> DependentInfantAirServicesOf(OrderAirTransportService airService)
        {
            var infantTravellerIds = _travellers
                .Where(traveller => traveller.InfantParentTravellerId == airService.TravellerId)
                .Select(traveller => traveller.Id)
                .ToHashSet();

            return _services
                .OfType<OrderAirTransportService>()
                .Where(service => service.IsActive
                                  && service.SegmentId == airService.SegmentId
                                  && infantTravellerIds.Contains(service.TravellerId));
        }

        private bool HasActiveService(OrderItem item)
            => _services.Any(service => service.OrderItemId == item.Id && service.IsActive);
    }
}
