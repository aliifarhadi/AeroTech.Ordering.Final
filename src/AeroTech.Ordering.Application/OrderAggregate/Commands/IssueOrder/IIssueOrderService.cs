using AeroTech.Ordering.Application._Shared.Authorization;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder
{
    public interface IIssueOrderService
    {
        Task<IssueOrderResult> IssueAsync(
            long orderId,
            long ticketDocumentStockId,
            IOrderAuthorization authorization,
            long? issuedByActorId,
            CancellationToken cancellationToken = default);
    }
}
