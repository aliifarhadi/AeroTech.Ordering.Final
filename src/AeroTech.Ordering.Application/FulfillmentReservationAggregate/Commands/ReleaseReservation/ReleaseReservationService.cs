using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public sealed class ReleaseReservationService : IReleaseReservationService
    {
        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IReservationProviderResolver _providers;
        private readonly IOrderReservationSummarizer _summarizer;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public ReleaseReservationService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IFulfillmentTaskRepository tasks,
            IReservationProviderResolver providers,
            IOrderReservationSummarizer summarizer,
            IReservationLock reservationLock,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _tasks = tasks;
            _providers = providers;
            _summarizer = summarizer;
            _reservationLock = reservationLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
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

            var intent = reservation.PrepareRelease();
            var task = await ReleaseTaskAsync(reservation, intent, cancellationToken);

            task.StartAttempt(ProviderInteractionType.ReleaseHold, _idGenerator, _clock.GetDateTime());
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var outcome = await _providers.Resolve(intent.ProviderKey).ReleaseAsync(intent, cancellationToken);
            var observedAt = _clock.GetDateTime();

            task.CompleteAttempt(AttemptOutcomeOf(outcome), outcome.Failure, observedAt);

            if (outcome.OperationOutcome == ProviderOperationOutcome.Succeeded)
            {
                reservation.RecordReleased(observedAt);
                await _summarizer.SummarizeAsync(order, reservations, cancellationToken);
                await _synchronizer.ProjectReservationChangedAsync(order.ToReadModelSnapshot(observedAt), cancellationToken);
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

        private async Task<FulfillmentTask> ReleaseTaskAsync(
            FulfillmentReservation reservation,
            ReleaseIntent intent,
            CancellationToken cancellationToken)
        {
            var latest = await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved, cancellationToken);

            if (latest is { IsResumable: true })
                return latest;

            var task = FulfillmentTask.Create(
                _idGenerator.NewId(),
                reservation.OrderId,
                reservation.Id,
                OrderFulfillmentTaskType.ReleaseReserved,
                reservation.FulfillmentProviderKey,
                intent.IdempotencyKey,
                reservation.CorrelationReference,
                reservation.Units.Select(unit => unit.Id).ToList(),
                OrderFulfillmentTargetAction.Release,
                _idGenerator,
                _clock.GetDateTime());

            await _tasks.AddAsync(task, cancellationToken);

            return task;
        }

        private static FulfillmentAttemptOutcome AttemptOutcomeOf(ReleaseOutcome outcome) => outcome.OperationOutcome switch
        {
            ProviderOperationOutcome.Succeeded => FulfillmentAttemptOutcome.Succeeded,
            ProviderOperationOutcome.Rejected => FulfillmentAttemptOutcome.Failed,
            _ => FulfillmentAttemptOutcome.Unknown
        };
    }
}
