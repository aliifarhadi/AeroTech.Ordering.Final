using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Messages.Ordering.IntegrationEvents.V1
{
    public record OrderCreated(
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
        DateTimeOffset CreatedAt) : BaseIntegrationEvent;
}
