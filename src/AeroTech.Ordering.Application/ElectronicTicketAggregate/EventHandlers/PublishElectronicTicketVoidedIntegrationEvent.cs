using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application._Shared.Events;
using MediatR;
using DomainEvent = AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents.ElectronicTicketVoided;
using IntegrationEvent = AeroTech.Messages.Ordering.IntegrationEvents.V1.ElectronicTicketVoided;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.EventHandlers
{
    public sealed class PublishElectronicTicketVoidedIntegrationEvent : INotificationHandler<DomainEventNotification<DomainEvent>>
    {
        private readonly IOutboxWriter _outboxWriter;

        public PublishElectronicTicketVoidedIntegrationEvent(IOutboxWriter outboxWriter) => _outboxWriter = outboxWriter;

        public Task Handle(DomainEventNotification<DomainEvent> notification, CancellationToken cancellationToken)
        {
            var @event = notification.DomainEvent;

            return _outboxWriter.WriteAsync(new IntegrationEvent(
                @event.ElectronicTicketId,
                @event.OrderId,
                @event.DocumentNumber,
                @event.VoidFulfillmentTaskId,
                @event.Reason,
                @event.ReasonText,
                @event.VoidedBy,
                @event.VoidedAt,
                @event.ProviderReference,
                @event.DocumentVersion), @event, cancellationToken);
        }
    }
}
