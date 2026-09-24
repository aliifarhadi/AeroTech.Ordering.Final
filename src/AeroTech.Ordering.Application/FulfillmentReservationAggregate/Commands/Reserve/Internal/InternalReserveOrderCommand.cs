using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Internal
{
    public sealed record InternalReserveOrderCommand(long OrderId) : IRequest<ReserveResult>, IReserveOrderCommand;
}
