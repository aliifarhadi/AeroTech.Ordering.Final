using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record BackofficeCreateOrderFromOfferRequest(
        string OfferId,
        long CustomerId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<OrderRemarkInput>? Remarks,
        string? AncillaryPricedSelectionId = null,
        string? SeatPricedSelectionId = null);

    public sealed record AgencyCreateOrderFromOfferRequest(
        string OfferId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<OrderRemarkInput>? Remarks,
        string? AncillaryPricedSelectionId = null,
        string? SeatPricedSelectionId = null);
}
