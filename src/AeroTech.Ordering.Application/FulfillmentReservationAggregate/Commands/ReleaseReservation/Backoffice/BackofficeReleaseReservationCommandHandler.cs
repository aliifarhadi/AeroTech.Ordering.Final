using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Backoffice
{
    public sealed class BackofficeReleaseReservationCommandHandler : IRequestHandler<BackofficeReleaseReservationCommand, ReleaseReservationResult>
    {
        private readonly IReleaseReservationService _service;

        public BackofficeReleaseReservationCommandHandler(IReleaseReservationService service) => _service = service;

        public Task<ReleaseReservationResult> Handle(BackofficeReleaseReservationCommand command, CancellationToken cancellationToken)
            => _service.ReleaseAsync(command.OrderId, command.ReservationId, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
