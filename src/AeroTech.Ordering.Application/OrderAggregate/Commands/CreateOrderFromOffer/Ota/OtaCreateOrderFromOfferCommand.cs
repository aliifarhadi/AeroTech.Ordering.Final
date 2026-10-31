using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Ota
{
    public sealed record OtaCreateOrderFromOfferCommand(
        string OfferId,
        OrderContact Contact,
        IReadOnlyList<OrderTraveller> Travelers,
        IReadOnlyList<OrderRemarkInput> Remarks,
        string? AncillaryPricedSelectionId,
        string? SeatPricedSelectionId) : IRequest<CreateOrderFromOfferResult>, IAgencyCreateOrderCommand;
}
