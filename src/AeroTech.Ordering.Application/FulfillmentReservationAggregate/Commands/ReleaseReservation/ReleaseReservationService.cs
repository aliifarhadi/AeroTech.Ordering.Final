using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public sealed class ReleaseReservationService : IReleaseReservationService
    {
        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IReservationReleaser _releaser;
        private readonly IOrderReservationSummarizer _summarizer;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ReleaseReservationService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IReservationReleaser releaser,
            IOrderReservationSummarizer summarizer,
            IReservationLock reservationLock,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _releaser = releaser;
            _summarizer = summarizer;
            _reservationLock = reservationLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<ReleaseReservationResult> ReleaseAsync(
            long orderId,
            long reservationId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var reservations = await _reservations.ListByOrderAsync(orderId, cancellationToken);
            var reservation = reservations.SingleOrDefault(item => item.Id == reservationId)
                              ?? throw ExceptionFactory.ReservationNotFound(reservationId, orderId);

            var outcome = await _releaser.ReleaseAsync(reservation, cancellationToken);

            if (outcome.OperationOutcome == ProviderOperationOutcome.Succeeded)
            {
                await _summarizer.SummarizeAsync(order, reservations, cancellationToken);
                await _synchronizer.ProjectReservationChangedAsync(order.ToReadModelSnapshot(_clock.GetDateTime()), cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ReleaseReservationResult(
                order.Id,
                order.Status,
                reservation.Id,
                reservation.Status,
                outcome.Failure?.Reason,
                outcome.Failure?.Message);
        }
    }
}
