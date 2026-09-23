using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Query._Shared.DbContexts;
using MassTransit;
using IntegrationEvent = AeroTech.Messages.Ordering.IntegrationEvents.V1.OrderCreated;

namespace AeroTech.Ordering.Consumers.Ordering.OrderAggregate.WhenOrderCreated
{
    public sealed class SyncQueryDbWhenOrderCreated : IConsumer<IntegrationEvent>
    {
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly OrderQueryDbContext _queryDbContext;

        public SyncQueryDbWhenOrderCreated(IOrderQueryDbSynchronizer synchronizer, OrderQueryDbContext queryDbContext)
        {
            _synchronizer = synchronizer;
            _queryDbContext = queryDbContext;
        }

        public async Task Consume(ConsumeContext<IntegrationEvent> context)
        {
            var @event = context.Message;

            await _synchronizer.ProjectCreatedAsync(new OrderReadModelSnapshot(
                @event.OrderId,
                @event.OrderReference,
                @event.SourceOfferId,
                @event.Status,
                @event.Channel,
                @event.CustomerId,
                @event.ActorId,
                TravelAgencyId: null,
                @event.OfficeKind,
                @event.OfficeId,
                @event.CurrencyId,
                @event.CustomerTotal,
                @event.CommercialVersion,
                @event.LastTicketingDate,
                @event.CreatedAt,
                @event.TimeOfOccurrence), context.CancellationToken);

            await _queryDbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}
