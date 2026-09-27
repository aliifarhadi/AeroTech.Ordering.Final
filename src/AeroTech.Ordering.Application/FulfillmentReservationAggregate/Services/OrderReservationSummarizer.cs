using AeroTech.Ordering.Application.OrderAggregate.Services;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class OrderReservationSummarizer : IOrderReservationSummarizer
    {
        private readonly IReservationProviderResolver _providers;
        private readonly IRecordLocatorAllocator _recordLocators;

        public OrderReservationSummarizer(IReservationProviderResolver providers, IRecordLocatorAllocator recordLocators)
        {
            _providers = providers;
            _recordLocators = recordLocators;
        }

        public async Task SummarizeAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            CancellationToken cancellationToken = default)
        {
            var latestUnitStatusByService = ReservationCoverage.LatestUnitStatusByService(reservations);

            order.SummarizeReservation(RequiredServiceIds(order), latestUnitStatusByService);

            if (order.RequiresRecordLocator(latestUnitStatusByService))
                order.AssignRecordLocator(await _recordLocators.AllocateAsync(cancellationToken));
        }

        public void SummarizeServicing(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlyCollection<ElectronicTicket> tickets)
            => order.SummarizeServicing(
                TicketCoverage.DocumentedServiceIds(tickets),
                RequiredServiceIds(order),
                ReservationCoverage.LatestUnitStatusByService(reservations));

        private IReadOnlyList<long> RequiredServiceIds(Order order)
            => order.Services
                .Where(_providers.RequiresReservation)
                .Select(service => service.Id)
                .ToList();
    }
}
