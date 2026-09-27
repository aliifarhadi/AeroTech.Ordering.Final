using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.IntegrationEvents.V1;
using AeroTech.Ordering.Application._Shared.Events;
using MediatR;
using DomainEvent = AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents.ElectronicTicketIssued;
using IntegrationEvent = AeroTech.Messages.Ordering.IntegrationEvents.V1.ElectronicTicketIssued;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.EventHandlers
{
    public sealed class PublishElectronicTicketIssuedIntegrationEvent : INotificationHandler<DomainEventNotification<DomainEvent>>
    {
        private readonly IOutboxWriter _outboxWriter;

        public PublishElectronicTicketIssuedIntegrationEvent(IOutboxWriter outboxWriter) => _outboxWriter = outboxWriter;

        public Task Handle(DomainEventNotification<DomainEvent> notification, CancellationToken cancellationToken)
        {
            var @event = notification.DomainEvent;

            return _outboxWriter.WriteAsync(new IntegrationEvent(
                @event.ElectronicTicketId,
                @event.OrderId,
                @event.TravellerId,
                @event.IssueFulfillmentTaskId,
                @event.DocumentNumber,
                @event.IssuerCarrierId,
                @event.IssuingOfficeId,
                @event.Authority,
                @event.CurrencyId,
                @event.IssuedTotal,
                @event.DocumentVersion,
                @event.PredecessorElectronicTicketId,
                @event.Coupons
                    .Select(coupon => new ElectronicTicketIssuedCoupon(
                        coupon.TicketCouponId,
                        coupon.CouponNumber,
                        coupon.OrderServiceId,
                        coupon.OrderSegmentId,
                        coupon.IssuanceValue,
                        coupon.PredecessorTicketCouponId))
                    .ToList()), @event, cancellationToken);
        }
    }
}
