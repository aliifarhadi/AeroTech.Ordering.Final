using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm.Backoffice
{
    public sealed class BackofficeConfirmReservedCapacityCommandHandler : IRequestHandler<BackofficeConfirmReservedCapacityCommand, ConfirmResult>
    {
        private readonly IConfirmService _service;

        public BackofficeConfirmReservedCapacityCommandHandler(IConfirmService service) => _service = service;

        public Task<ConfirmResult> Handle(BackofficeConfirmReservedCapacityCommand command, CancellationToken cancellationToken)
            => _service.ConfirmReservedCapacityAsync(command.OrderId, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
