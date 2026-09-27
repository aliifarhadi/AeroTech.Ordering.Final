using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain._Shared.Contracts;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder.Backoffice
{
    public sealed class BackofficeIssueOrderCommandHandler : IRequestHandler<BackofficeIssueOrderCommand, IssueOrderResult>
    {
        private readonly IIssueOrderService _service;
        private readonly ICallerContext _callerContext;

        public BackofficeIssueOrderCommandHandler(IIssueOrderService service, ICallerContext callerContext)
        {
            _service = service;
            _callerContext = callerContext;
        }

        public Task<IssueOrderResult> Handle(BackofficeIssueOrderCommand command, CancellationToken cancellationToken)
            => _service.IssueAsync(
                command.OrderId,
                command.TicketDocumentStockId,
                UnrestrictedOrderAuthorization.Instance,
                _callerContext.ActorId,
                cancellationToken);
    }
}
