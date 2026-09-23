using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Backoffice
{
    public sealed record BackofficeCreateOrderFromOfferCommand(
        string OfferId,
        long CustomerId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<SeatSelection> SeatSelections,
        IReadOnlyList<OrderRemarkInput> Remarks) : IRequest<CreateOrderFromOfferResult>;
}
