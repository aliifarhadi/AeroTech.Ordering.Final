using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm.Backoffice
{
    public sealed class BackofficeConfirmReservationsCommandHandler : IRequestHandler<BackofficeConfirmReservationsCommand, ConfirmResult>
    {
        private readonly IConfirmService _service;

        public BackofficeConfirmReservationsCommandHandler(IConfirmService service) => _service = service;

        public Task<ConfirmResult> Handle(BackofficeConfirmReservationsCommand command, CancellationToken cancellationToken)
            => _service.ConfirmReservationsAsync(command.OrderId, command.ReservationIds, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
