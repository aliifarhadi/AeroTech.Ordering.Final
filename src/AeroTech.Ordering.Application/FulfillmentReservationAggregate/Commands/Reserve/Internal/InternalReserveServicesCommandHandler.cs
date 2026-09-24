using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Internal
{
    public sealed class InternalReserveServicesCommandHandler : IRequestHandler<InternalReserveServicesCommand, ReserveResult>
    {
        private readonly IReserveService _service;

        public InternalReserveServicesCommandHandler(IReserveService service) => _service = service;

        public Task<ReserveResult> Handle(InternalReserveServicesCommand command, CancellationToken cancellationToken)
            => _service.ReserveServicesAsync(command.OrderId, command.OrderServiceIds, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
