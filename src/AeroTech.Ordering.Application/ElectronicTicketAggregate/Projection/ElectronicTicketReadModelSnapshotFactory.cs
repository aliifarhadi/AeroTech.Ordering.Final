using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Projection
{
    internal static class ElectronicTicketReadModelSnapshotFactory
    {
        public static ElectronicTicketReadModelSnapshot ToReadModelSnapshot(this ElectronicTicket ticket)
            => new(
                ticket.Id,
                ticket.CurrentServicingOrderId,
                ticket.TravellerId,
                ticket.DocumentNumber,
                ticket.Authority,
                ticket.IssuedAt,
                ticket.IssuedTotal,
                ticket.CurrencyId,
                ticket.StatusSummary,
                ticket.DocumentVersion,
                ticket.Coupons.OrderBy(coupon => coupon.CouponNumber).Select(ToSnapshot).ToList());

        private static TicketCouponReadModelSnapshot ToSnapshot(TicketCoupon coupon)
            => new(
                coupon.Id,
                coupon.CouponNumber,
                coupon.OriginalOrderServiceId,
                coupon.CurrentOrderServiceId,
                coupon.OrderSegmentId,
                coupon.FareBasisSnapshot,
                coupon.BookingClassSnapshot,
                coupon.RbdIdSnapshot,
                coupon.CabinClassIdSnapshot,
                coupon.IssuanceValue,
                coupon.CurrencyId,
                coupon.FinancialStatus,
                coupon.ControlStatus);
    }
}
