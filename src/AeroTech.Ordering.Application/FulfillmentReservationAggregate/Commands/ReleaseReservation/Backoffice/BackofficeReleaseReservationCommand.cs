using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Backoffice
{
    public sealed record BackofficeReleaseReservationCommand(
        long OrderId,
        long ReservationId) : IRequest<ReleaseReservationResult>, IReleaseReservationCommand;
}
