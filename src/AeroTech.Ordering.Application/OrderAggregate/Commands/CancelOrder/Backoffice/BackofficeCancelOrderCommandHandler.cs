using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Application._Shared.Caller;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder.Backoffice
{
    public sealed class BackofficeCancelOrderCommandHandler : IRequestHandler<BackofficeCancelOrderCommand, CancelOrderResult>
    {
        private const SalesChannel Channel = SalesChannel.BackOffice;

        private readonly ICancelOrderService _service;
        private readonly ISalesContextFactory _salesContextFactory;

        public BackofficeCancelOrderCommandHandler(ICancelOrderService service, ISalesContextFactory salesContextFactory)
        {
            _service = service;
            _salesContextFactory = salesContextFactory;
        }

        public Task<CancelOrderResult> Handle(BackofficeCancelOrderCommand command, CancellationToken cancellationToken)
            => _service.CancelAsync(
                command.OrderId,
                command.ServiceIds ?? [],
                command.Reason,
                UnrestrictedOrderAuthorization.Instance,
                _salesContextFactory.Create(Channel),
                cancellationToken);
    }
}
