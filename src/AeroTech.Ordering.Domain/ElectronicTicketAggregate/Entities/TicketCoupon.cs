using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities
{
    public sealed class TicketCoupon : Entity<long>
    {
        private TicketCoupon()
        {
        }

        internal TicketCoupon(
            long id,
            long ticketId,
            int couponNumber,
            long orderServiceId,
            long orderSegmentId,
            long? orderFareComponentId,
            IssuedSegmentSnapshot issuedSegment,
            string? fareBasisSnapshot,
            string? bookingClassSnapshot,
            long? rbdIdSnapshot,
            long? cabinClassIdSnapshot,
            BaggageAllowance? baggageAllowanceSnapshot,
            decimal issuanceValue,
            int currencyId)
        {
            Id = id;
            TicketId = ticketId;
            CouponNumber = couponNumber;
            OriginalOrderServiceId = orderServiceId;
            CurrentOrderServiceId = orderServiceId;
            OrderSegmentId = orderSegmentId;
            OrderFareComponentId = orderFareComponentId;
            IssuedSegment = issuedSegment;
            FareBasisSnapshot = fareBasisSnapshot;
            BookingClassSnapshot = bookingClassSnapshot;
            RbdIdSnapshot = rbdIdSnapshot;
            CabinClassIdSnapshot = cabinClassIdSnapshot;
            BaggageAllowanceSnapshot = baggageAllowanceSnapshot;
            IssuanceValue = issuanceValue;
            CurrencyId = currencyId;
            FinancialStatus = TicketCouponFinancialStatus.Open;
            ControlStatus = TicketCouponControlStatus.Local;
        }

        public long TicketId { get; private set; }

        public int CouponNumber { get; private set; }

        public long OriginalOrderServiceId { get; private set; }

        public long CurrentOrderServiceId { get; private set; }

        public long OrderSegmentId { get; private set; }

        public long? OrderFareComponentId { get; private set; }

        public long? PredecessorTicketCouponId { get; private set; }

        public IssuedSegmentSnapshot IssuedSegment { get; private set; } = default!;

        public string? FareBasisSnapshot { get; private set; }

        public string? BookingClassSnapshot { get; private set; }

        public long? RbdIdSnapshot { get; private set; }

        public long? CabinClassIdSnapshot { get; private set; }

        public BaggageAllowance? BaggageAllowanceSnapshot { get; private set; }

        public decimal IssuanceValue { get; private set; }

        public int CurrencyId { get; private set; }

        public TicketCouponFinancialStatus FinancialStatus { get; private set; }

        public TicketCouponControlStatus ControlStatus { get; private set; }

        public string? ProviderCouponStatusCode { get; private set; }

        public DateOnly? NotValidBefore { get; private set; }

        public DateOnly? NotValidAfter { get; private set; }

        public DateTimeOffset? UsedAt { get; private set; }

        public string? UsageReference { get; private set; }

        internal void EnsureVoidable(long electronicTicketId)
        {
            if (FinancialStatus != TicketCouponFinancialStatus.Open || UsedAt is not null)
                throw ExceptionFactory.TicketCouponIsNotVoidable(
                    electronicTicketId,
                    CouponNumber,
                    UsedAt is null ? FinancialStatus : TicketCouponFinancialStatus.Used);

            if (ControlStatus != TicketCouponControlStatus.Local)
                throw ExceptionFactory.TicketCouponControlIsNotLocal(electronicTicketId, CouponNumber, ControlStatus);
        }

        internal void MarkVoid() => FinancialStatus = TicketCouponFinancialStatus.Void;
    }
}
