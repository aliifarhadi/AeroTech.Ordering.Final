using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public sealed record VoidElectronicTicketsResult(
        long OrderId,
        OrderStatus OrderStatus,
        long? VoidFulfillmentTaskId,
        IReadOnlyList<VoidedTicketResult> Tickets);

    public sealed record VoidedTicketResult(
        long ElectronicTicketId,
        string DocumentNumber,
        ElectronicTicketStatus Status,
        int DocumentVersion,
        long? VoidFulfillmentTaskId,
        DateTimeOffset? VoidedAt,
        IReadOnlyList<VoidedCouponResult> Coupons);

    public sealed record VoidedCouponResult(
        long TicketCouponId,
        int CouponNumber,
        TicketCouponFinancialStatus FinancialStatus);
}
