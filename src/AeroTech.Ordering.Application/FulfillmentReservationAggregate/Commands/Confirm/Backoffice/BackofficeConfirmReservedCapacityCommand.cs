using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm.Backoffice
{
    public sealed record BackofficeConfirmReservedCapacityCommand(long OrderId) : IRequest<ConfirmResult>, IConfirmReservedCapacityCommand;
}
