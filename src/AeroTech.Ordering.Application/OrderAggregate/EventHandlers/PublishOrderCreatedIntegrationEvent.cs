using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application._Shared.Events;
using AeroTech.Ordering.Domain.OrderAggregate.DomainEvents;
using MediatR;
using IntegrationEvent = AeroTech.Messages.Ordering.IntegrationEvents.V1.OrderCreated;

namespace AeroTech.Ordering.Application.OrderAggregate.EventHandlers
{
    public sealed class PublishOrderCreatedIntegrationEvent : INotificationHandler<DomainEventNotification<OrderCreated>>
    {
        private readonly IOutboxWriter _outboxWriter;

        public PublishOrderCreatedIntegrationEvent(IOutboxWriter outboxWriter) => _outboxWriter = outboxWriter;

        public Task Handle(DomainEventNotification<OrderCreated> notification, CancellationToken cancellationToken)
        {
            var @event = notification.DomainEvent;

            return _outboxWriter.WriteAsync(new IntegrationEvent(
                @event.OrderId,
                @event.OrderReference,
                @event.SourceOfferId,
                @event.CustomerId,
                @event.Channel,
                @event.ActorId,
                @event.OfficeKind,
                @event.OfficeId,
                @event.Status,
                @event.CurrencyId,
                @event.CustomerTotal,
                @event.CommercialVersion,
                @event.TravellerCount,
                @event.LastTicketingDate,
                @event.CreatedAt), @event, cancellationToken);
        }
    }
}
