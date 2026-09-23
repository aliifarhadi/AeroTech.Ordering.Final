using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public sealed record AddRemarkResult(
        long OrderId,
        long RemarkId,
        OrderRemarkStatus Status,
        long? SupersedesRemarkId,
        int CommercialVersion);
}
