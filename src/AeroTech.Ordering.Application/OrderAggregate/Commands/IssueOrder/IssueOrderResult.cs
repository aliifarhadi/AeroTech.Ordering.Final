using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder
{
    public sealed record IssueOrderResult(
        long OrderId,
        OrderStatus OrderStatus,
        long IssueFulfillmentTaskId,
        IReadOnlyList<IssuedTicketResult> Tickets);

    public sealed record IssuedTicketResult(
        long ElectronicTicketId,
        long TravellerId,
        string DocumentNumber,
        ElectronicTicketStatus Status,
        DateTimeOffset IssuedAt,
        decimal IssuedTotal,
        int CurrencyId,
        IReadOnlyList<IssuedCouponResult> Coupons);

    public sealed record IssuedCouponResult(
        long TicketCouponId,
        int CouponNumber,
        long OrderServiceId,
        long OrderSegmentId,
        TicketCouponFinancialStatus FinancialStatus,
        TicketCouponControlStatus ControlStatus,
        decimal IssuanceValue);
}
