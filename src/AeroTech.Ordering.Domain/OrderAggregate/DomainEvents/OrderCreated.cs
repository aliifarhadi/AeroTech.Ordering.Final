using AeroTech.Framework.Core.Domain.Events;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.DomainEvents
{
    public sealed record OrderCreated(
        string EventId,
        string AggregateId,
        DateTimeOffset TimeOfOccurrence,
        long OrderId,
        Guid OrderReference,
        string SourceOfferId,
        long CustomerId,
        SalesChannel Channel,
        long ActorId,
        SellingOfficeKind? OfficeKind,
        long? OfficeId,
        OrderStatus Status,
        int CurrencyId,
        decimal CustomerTotal,
        int CommercialVersion,
        int TravellerCount,
        DateTimeOffset? LastTicketingDate,
        DateTimeOffset CreatedAt) : DomainEvent(EventId, AggregateId, TimeOfOccurrence);
}
