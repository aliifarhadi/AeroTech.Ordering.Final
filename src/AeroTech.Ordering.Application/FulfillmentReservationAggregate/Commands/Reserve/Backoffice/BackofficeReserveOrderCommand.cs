using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Backoffice
{
    public sealed record BackofficeReserveOrderCommand(long OrderId) : IRequest<ReserveResult>, IReserveOrderCommand;
}
