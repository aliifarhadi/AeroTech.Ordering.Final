using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        public IReadOnlyList<OrderAirTransportService> TicketableAirServices()
            => _services
                .OfType<OrderAirTransportService>()
                .Where(service => service.CommercialStatus == OrderServiceCommercialState.Active)
                .ToList();

        public void EnsureIssuableAt(DateTimeOffset now)
        {
            if (HasPassedLastTicketingDateAt(now))
                throw ExceptionFactory.OrderCannotBeIssuedAfterLastTicketingDate(Id, LastTicketingDate);
        }

        public void EnsureAcceptedPricingIsIntactFor(IReadOnlyCollection<long> airServiceIds)
        {
            var activeAirServiceIds = TicketableAirServices().Select(service => service.Id).ToHashSet();

            if (FarePricingAtomsCovering(airServiceIds).FirstOrDefault(atom => atom.IsFracturedAmong(activeAirServiceIds)) is { } fractured)
                throw ExceptionFactory.RepricingRequiredAfterPartialCancellation(Id, fractured.OrderFarePricingUnitId);
        }

        public void MarkTicketed(IReadOnlySet<long> documentedServiceIds)
        {
            if (TicketableAirServices().FirstOrDefault(service => !documentedServiceIds.Contains(service.Id)) is { } undocumented)
                throw ExceptionFactory.OrderIsNotFullyTicketed(Id, undocumented.Id);

            TransitionTo(OrderStatus.Ticketed);
        }
    }
}
