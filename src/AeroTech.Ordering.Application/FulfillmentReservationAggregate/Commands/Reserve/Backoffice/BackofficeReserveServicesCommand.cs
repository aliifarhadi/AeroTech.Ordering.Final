using MediatR;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Backoffice
{
    public sealed record BackofficeReserveServicesCommand(
        long OrderId,
        IReadOnlyCollection<long> OrderServiceIds) : IRequest<ReserveResult>, IReserveServicesCommand;
}
