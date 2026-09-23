using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record BackofficeCreateOrderFromOfferRequest(
        string OfferId,
        long CustomerId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<SeatSelection>? SeatSelections,
        IReadOnlyList<OrderRemarkInput>? Remarks);

    public sealed record AgencyCreateOrderFromOfferRequest(
        string OfferId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<SeatSelection>? SeatSelections,
        IReadOnlyList<OrderRemarkInput>? Remarks);
}
