using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record CancelOrderRequest(IReadOnlyList<long>? ServiceIds, VoidReason Reason, string? ReasonDetail);
}
