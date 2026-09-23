namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public interface IAgencyCreateOrderCommand
    {
        string OfferId { get; }

        OrderContact Contact { get; }

        IReadOnlyList<OrderTraveller> Travelers { get; }

        IReadOnlyList<SeatSelection> SeatSelections { get; }

        IReadOnlyList<OrderRemarkInput> Remarks { get; }
    }
}
