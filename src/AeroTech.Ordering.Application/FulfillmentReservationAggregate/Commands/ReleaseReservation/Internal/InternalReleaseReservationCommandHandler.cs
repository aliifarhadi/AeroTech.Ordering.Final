using AeroTech.Ordering.Application._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Internal
{
    public sealed class InternalReleaseReservationCommandHandler : IRequestHandler<InternalReleaseReservationCommand, ReleaseReservationResult>
    {
        private readonly IReleaseReservationService _service;

        public InternalReleaseReservationCommandHandler(IReleaseReservationService service) => _service = service;

        public Task<ReleaseReservationResult> Handle(InternalReleaseReservationCommand command, CancellationToken cancellationToken)
            => _service.ReleaseAsync(command.OrderId, command.ReservationId, UnrestrictedOrderAuthorization.Instance, cancellationToken);
    }
}
