using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm.Backoffice
{
    public sealed record BackofficeConfirmReservationsCommand(
        long OrderId,
        IReadOnlyCollection<long> ReservationIds) : IRequest<ConfirmResult>, IConfirmReservationsCommand;
}
