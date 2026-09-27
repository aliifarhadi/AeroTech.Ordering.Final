using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;

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
                ticket.VoidDeadline,
                ToSnapshot(ticket.VoidRecord),
                ticket.Coupons.OrderBy(coupon => coupon.CouponNumber).Select(ToSnapshot).ToList());

        private static DocumentVoidRecordSnapshot? ToSnapshot(DocumentVoidRecord? voidRecord)
            => voidRecord is null
                ? null
                : new DocumentVoidRecordSnapshot(
                    voidRecord.VoidFulfillmentTaskId,
                    voidRecord.ReasonCode,
                    voidRecord.ReasonText,
                    voidRecord.ProviderReference,
                    voidRecord.ActorId,
                    voidRecord.VoidedAt);

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
