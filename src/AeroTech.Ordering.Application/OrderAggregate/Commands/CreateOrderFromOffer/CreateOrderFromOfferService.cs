using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.DomainEvents;
using AeroTech.Ordering.Domain.Providers.Offer;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed class CreateOrderFromOfferService : ICreateOrderFromOfferService
    {
        private readonly IOfferProvider _offerProvider;
        private readonly IOrderRepository _orders;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public CreateOrderFromOfferService(
            IOfferProvider offerProvider,
            IOrderRepository orders,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _offerProvider = offerProvider;
            _orders = orders;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<CreateOrderFromOfferResult> ExecuteAsync(
            CreateOrderArgs args,
            CancellationToken cancellationToken = default)
        {
            var offer = await _offerProvider.GetByOfferIdAsync(args.OfferId, cancellationToken);

            var order = Order.Create(args, offer, _idGenerator, _clock);

            await _orders.AddAsync(order, cancellationToken);

            var created = order.GetEvents().OfType<OrderCreated>().Single();
            await _synchronizer.ProjectCreatedAsync(order.ToReadModelSnapshot(created.TimeOfOccurrence), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateOrderFromOfferResult(
                order.Id,
                order.OrderReference,
                order.CurrencyId,
                order.CustomerTotal,
                order.CommercialVersion);
        }
    }
}
