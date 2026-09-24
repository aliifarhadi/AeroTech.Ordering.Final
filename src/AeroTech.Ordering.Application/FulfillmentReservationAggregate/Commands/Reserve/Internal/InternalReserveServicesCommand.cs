using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Internal
{
    public sealed record InternalReserveServicesCommand(
        long OrderId,
        IReadOnlyCollection<long> OrderServiceIds) : IRequest<ReserveResult>, IReserveServicesCommand;
}
