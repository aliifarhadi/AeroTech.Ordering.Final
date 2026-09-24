using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Backoffice
{
    public sealed class BackofficeReserveOrderCommandHandler : IRequestHandler<BackofficeReserveOrderCommand, ReserveResult>
    {
        private readonly IReserveService _service;

        public BackofficeReserveOrderCommandHandler(IReserveService service) => _service = service;

        public Task<ReserveResult> Handle(BackofficeReserveOrderCommand command, CancellationToken cancellationToken)
            => _service.ReserveOrderAsync(command.OrderId, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
