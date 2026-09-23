using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.OtaPanel
{
    public sealed record OtaPanelCreateOrderFromOfferCommand(
        string OfferId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<SeatSelection> SeatSelections,
        IReadOnlyList<OrderRemarkInput> Remarks) : IRequest<CreateOrderFromOfferResult>, IAgencyCreateOrderCommand;
}
