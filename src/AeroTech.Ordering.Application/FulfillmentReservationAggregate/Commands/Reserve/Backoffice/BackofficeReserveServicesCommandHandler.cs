using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Backoffice
{
    public sealed class BackofficeReserveServicesCommandHandler : IRequestHandler<BackofficeReserveServicesCommand, ReserveResult>
    {
        private readonly IReserveService _service;

        public BackofficeReserveServicesCommandHandler(IReserveService service) => _service = service;

        public Task<ReserveResult> Handle(BackofficeReserveServicesCommand command, CancellationToken cancellationToken)
            => _service.ReserveServicesAsync(command.OrderId, command.OrderServiceIds, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
