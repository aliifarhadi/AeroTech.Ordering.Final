using AeroTech.Framework.Core.Domain.Entities;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities
{
    public sealed class TicketPriceLink : Entity<long>
    {
        private TicketPriceLink()
        {
        }

        internal TicketPriceLink(
            long id,
            long electronicTicketId,
            long? ticketCouponId,
            long pricingLineId,
            long? pricingAllocationId,
            decimal attributedValue,
            int currencyId)
        {
            Id = id;
            ElectronicTicketId = electronicTicketId;
            TicketCouponId = ticketCouponId;
            PricingLineId = pricingLineId;
            PricingAllocationId = pricingAllocationId;
            AttributedValue = attributedValue;
            CurrencyId = currencyId;
        }

        public long ElectronicTicketId { get; private set; }

        public long? TicketCouponId { get; private set; }

        public long PricingLineId { get; private set; }

        public long? PricingAllocationId { get; private set; }

        public decimal AttributedValue { get; private set; }

        public int CurrencyId { get; private set; }
    }
}
