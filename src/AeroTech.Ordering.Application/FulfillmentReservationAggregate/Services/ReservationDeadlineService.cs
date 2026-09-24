using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class ReservationDeadlineService : IReservationDeadlineService
    {
        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IReservationProviderResolver _providers;
        private readonly IReservationReleaser _releaser;
        private readonly IOrderReservationSummarizer _summarizer;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;

        public ReservationDeadlineService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IReservationProviderResolver providers,
            IReservationReleaser releaser,
            IOrderReservationSummarizer summarizer,
            IReservationLock reservationLock,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _providers = providers;
            _releaser = releaser;
            _summarizer = summarizer;
            _reservationLock = reservationLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<IReadOnlyList<long>> ListDueOrderIdsAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var now = _clock.GetDateTime();
            var withDueHolds = await _reservations.ListOrderIdsWithDueHoldsAsync(now, batchSize, cancellationToken);
            var pastLastTicketingDate = await _orders.ListReservableIdsPastLastTicketingDateAsync(now, batchSize, cancellationToken);

            return withDueHolds.Union(pastLastTicketingDate).Take(batchSize).ToList();
        }

        public async Task EnforceAsync(long orderId, CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            var reservations = await _reservations.ListByOrderAsync(orderId, cancellationToken);
            var statusBefore = order.Status;
            var settled = false;

            foreach (var reservation in reservations.Where(item => item.Status == FulfillmentReservationStatus.Held))
                settled |= await SettleAsync(order, reservation, cancellationToken);

            await _summarizer.SummarizeAsync(order, reservations, cancellationToken);

            var now = _clock.GetDateTime();

            if (reservations.All(reservation => reservation.IsSettled) && order.IsExpirableAt(now, RequiredServiceIds(order), ValidationTimeLimitByService(reservations)))
                order.Expire();

            if (settled || order.Status != statusBefore)
                await _synchronizer.ProjectReservationChangedAsync(order.ToReadModelSnapshot(now), cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> SettleAsync(Order order, FulfillmentReservation reservation, CancellationToken cancellationToken)
        {
            var now = _clock.GetDateTime();

            if (reservation.HoldLapsedAt(now) && _providers.CapabilityOf(order, reservation).ExpiresAutomatically)
            {
                reservation.RecordExpired(now);
                return true;
            }

            if (!reservation.DeadlinePassedAt(now, order.LastTicketingDate) || await _releaser.ReleaseWasRejectedAsync(reservation, cancellationToken))
                return false;

            var outcome = await _releaser.ReleaseAsync(reservation, cancellationToken);

            return outcome.OperationOutcome == ProviderOperationOutcome.Succeeded;
        }

        private IReadOnlyCollection<long> RequiredServiceIds(Order order)
            => order.Services
                .Where(_providers.RequiresReservation)
                .Select(service => service.Id)
                .ToList();

        private static IReadOnlyDictionary<long, DateTimeOffset?> ValidationTimeLimitByService(IEnumerable<FulfillmentReservation> reservations)
            => ReservationCoverage.LatestReservationByService(reservations)
                .ToDictionary(coverage => coverage.Key, coverage => coverage.Value.ReservationValidationTimeLimit);
    }
}
