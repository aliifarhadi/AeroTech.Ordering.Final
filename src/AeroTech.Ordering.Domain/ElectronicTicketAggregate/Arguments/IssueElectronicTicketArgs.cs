using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments
{
    public sealed record IssueElectronicTicketArgs(
        long OrderId,
        long TravellerId,
        long TravellerProfileRevisionId,
        long IssueFulfillmentTaskId,
        string DocumentNumber,
        DocumentIssuanceContext IssuanceContext,
        int CurrencyId,
        IReadOnlyList<IssueTicketCouponArgs> Coupons);

    public sealed record IssueTicketCouponArgs(
        long OrderServiceId,
        long TravellerId,
        long OrderSegmentId,
        long? OrderFareComponentId,
        IssuedSegmentSnapshot IssuedSegment,
        string? FareBasis,
        string? BookingClass,
        long? RbdId,
        long? CabinClassId,
        BaggageAllowance? BaggageAllowance,
        IReadOnlyList<IssueTicketPriceLinkArgs> PriceLinks);

    public sealed record IssueTicketPriceLinkArgs(
        long PricingLineId,
        long? PricingAllocationId,
        decimal AttributedValue,
        int CurrencyId);
}
