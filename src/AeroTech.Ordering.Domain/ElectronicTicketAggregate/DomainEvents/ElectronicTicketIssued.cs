using AeroTech.Framework.Core.Domain.Events;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents
{
    public sealed record ElectronicTicketIssued(
        string EventId,
        string AggregateId,
        DateTimeOffset TimeOfOccurrence,
        long ElectronicTicketId,
        long OrderId,
        long TravellerId,
        long IssueFulfillmentTaskId,
        string DocumentNumber,
        int IssuerCarrierId,
        long? IssuingOfficeId,
        DocumentAuthority Authority,
        int CurrencyId,
        decimal IssuedTotal,
        int DocumentVersion,
        long? PredecessorElectronicTicketId,
        IReadOnlyList<IssuedTicketCoupon> Coupons) : DomainEvent(EventId, AggregateId, TimeOfOccurrence);

    public sealed record IssuedTicketCoupon(
        long TicketCouponId,
        int CouponNumber,
        long OrderServiceId,
        long OrderSegmentId,
        decimal IssuanceValue,
        long? PredecessorTicketCouponId);
}
