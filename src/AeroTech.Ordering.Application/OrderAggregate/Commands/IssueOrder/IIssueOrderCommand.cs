namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder
{
    public interface IIssueOrderCommand
    {
        long OrderId { get; }

        long TicketDocumentStockId { get; }
    }
}
