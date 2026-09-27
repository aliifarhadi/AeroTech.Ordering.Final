using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder
{
    public interface ICancelOrderCommand
    {
        long OrderId { get; }

        IReadOnlyList<long>? ServiceIds { get; }

        VoidReason Reason { get; }

        string? ReasonDetail { get; }
    }
}
