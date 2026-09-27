using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts
{
    public sealed record ElectronicTicketReadModelSnapshot(
        long TicketId,
        long OrderId,
        long TravellerId,
        string DocumentNumber,
        DocumentAuthority Authority,
        DateTimeOffset IssuedAt,
        decimal IssuedTotal,
        int CurrencyId,
        ElectronicTicketStatus StatusSummary,
        int DocumentVersion,
        IReadOnlyList<TicketCouponReadModelSnapshot> Coupons);

    public sealed record TicketCouponReadModelSnapshot(
        long CouponId,
        int CouponNumber,
        long OriginalOrderServiceId,
        long CurrentOrderServiceId,
        long OrderSegmentId,
        string? FareBasisSnapshot,
        string? BookingClassSnapshot,
        long? RbdIdSnapshot,
        long? CabinClassIdSnapshot,
        decimal IssuanceValue,
        int CurrencyId,
        TicketCouponFinancialStatus FinancialStatus,
        TicketCouponControlStatus ControlStatus);
}
