using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate
{
    public static class TicketCoverage
    {
        public static IReadOnlySet<long> DocumentedServiceIds(IEnumerable<ElectronicTicket> tickets)
            => tickets
                .Where(ticket => !ticket.IsVoided)
                .SelectMany(ticket => ticket.Coupons)
                .Where(coupon => coupon.FinancialStatus != TicketCouponFinancialStatus.Void)
                .Select(coupon => coupon.CurrentOrderServiceId)
                .ToHashSet();
    }
}
