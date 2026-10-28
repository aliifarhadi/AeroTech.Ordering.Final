using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries
{
    public sealed class AddAncillariesService : IAddAncillariesService
    {
        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IElectronicTicketRepository _tickets;
        private readonly IAncillaryProvider _ancillaries;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public AddAncillariesService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IElectronicTicketRepository tickets,
            IAncillaryProvider ancillaries,
            IReservationLock reservationLock,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _tickets = tickets;
            _ancillaries = ancillaries;
            _reservationLock = reservationLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<AddAncillariesResult> AddAsync(
            long orderId,
            int expectedCommercialVersion,
            string selectionId,
            IOrderAuthorization authorization,
            SalesContext actorContext,
            CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var hasFulfillmentHistory = (await _reservations.ListByOrderAsync(orderId, cancellationToken)).Count > 0
                                        || (await _tickets.ListByOrderAsync(orderId, cancellationToken)).Count > 0;

            order.EnsureAcceptsAncillaries(expectedCommercialVersion, hasFulfillmentHistory, _clock.GetDateTime());

            var selection = await _ancillaries.ResolveSelectionAsync(selectionId, cancellationToken);
            var change = order.AddAncillaries(
                selection,
                null,
                expectedCommercialVersion,
                hasFulfillmentHistory,
                actorContext,
                _idGenerator,
                _clock.GetDateTime());

            await _synchronizer.ProjectServicedAsync(order.ToReadModelSnapshot(change.CommittedAt), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AddAncillariesResult(
                order.Id,
                change.Id,
                order.CommercialVersion,
                order.CurrencyId,
                order.CustomerTotal,
                order.Services
                    .OfType<OrderAncillaryService>()
                    .Where(service => service.CreatedByChangeId == change.Id)
                    .Select(service => new AddedAncillaryService(service.Id, service.ServiceType, service.VariantCode))
                    .ToList());
        }
    }
}
