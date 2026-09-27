using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder.Backoffice
{
    public sealed record BackofficeIssueOrderCommand(long OrderId, long TicketDocumentStockId) : IRequest<IssueOrderResult>, IIssueOrderCommand;
}
