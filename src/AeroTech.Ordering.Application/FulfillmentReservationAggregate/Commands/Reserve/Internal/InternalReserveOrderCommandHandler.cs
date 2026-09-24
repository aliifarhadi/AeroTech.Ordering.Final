using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Internal
{
    public sealed class InternalReserveOrderCommandHandler : IRequestHandler<InternalReserveOrderCommand, ReserveResult>
    {
        private readonly IReserveService _service;

        public InternalReserveOrderCommandHandler(IReserveService service) => _service = service;

        public Task<ReserveResult> Handle(InternalReserveOrderCommand command, CancellationToken cancellationToken)
            => _service.ReserveOrderAsync(command.OrderId, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
