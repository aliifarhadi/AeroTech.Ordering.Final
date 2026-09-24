using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public interface IOrderReservationSummarizer
    {
        Task SummarizeAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            CancellationToken cancellationToken = default);
    }
}
