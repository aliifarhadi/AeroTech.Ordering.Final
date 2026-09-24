using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Internal
{
    public sealed record InternalReleaseReservationCommand(
        long OrderId,
        long ReservationId) : IRequest<ReleaseReservationResult>, IReleaseReservationCommand;
}
