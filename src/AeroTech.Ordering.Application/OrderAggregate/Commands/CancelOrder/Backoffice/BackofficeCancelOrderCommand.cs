using AeroTech.Messages.Ordering.Enums;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder.Backoffice
{
    public sealed record BackofficeCancelOrderCommand(
        long OrderId,
        IReadOnlyList<long>? ServiceIds,
        VoidReason Reason,
        string? ReasonDetail) : IRequest<CancelOrderResult>, ICancelOrderCommand;
}
