using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder
{
    public interface ICancelOrderService
    {
        Task<CancelOrderResult> CancelAsync(
            long orderId,
            IReadOnlyCollection<long> serviceIds,
            VoidReason reason,
            IOrderAuthorization authorization,
            SalesContext actorContext,
            CancellationToken cancellationToken = default);
    }
}
