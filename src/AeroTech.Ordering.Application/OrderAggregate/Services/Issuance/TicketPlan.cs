using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Issuance
{
    public sealed record TicketPlan(
        long TravellerId,
        long TravellerProfileRevisionId,
        string DocumentRole,
        IReadOnlyList<IssueTicketCouponArgs> Coupons);
}
