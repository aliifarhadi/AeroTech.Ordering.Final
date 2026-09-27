using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Models;

namespace AeroTech.Ordering.Query.ElectronicTicketAggregate.Models
{
    public sealed class TicketCouponReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long ElectronicTicketId { get; set; }

        public int CouponNumber { get; set; }

        public long OriginalOrderServiceId { get; set; }

        public long CurrentOrderServiceId { get; set; }

        public long OrderSegmentId { get; set; }

        public string? FareBasisSnapshot { get; set; }

        public string? BookingClassSnapshot { get; set; }

        public long? RbdIdSnapshot { get; set; }

        public long? CabinClassIdSnapshot { get; set; }

        public decimal IssuanceValue { get; set; }

        public int CurrencyId { get; set; }

        public TicketCouponFinancialStatus FinancialStatus { get; set; }

        public TicketCouponControlStatus ControlStatus { get; set; }
    }
}
