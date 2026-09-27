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
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public sealed class ConfirmService : IConfirmService
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

        public ConfirmService(
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

        public Task<ConfirmResult> ConfirmReservedCapacityAsync(
            long orderId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => ConfirmAsync(orderId, authorization, ReservationsInScope, cancellationToken);

        public Task<ConfirmResult> ConfirmReservationsAsync(
            long orderId,
            IReadOnlyCollection<long> reservationIds,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => ConfirmAsync(orderId, authorization, reservations => RequestedReservations(orderId, reservations, reservationIds), cancellationToken);

        private async Task<ConfirmResult> ConfirmAsync(
            long orderId,
            IOrderAuthorization authorization,
            Func<IReadOnlyList<FulfillmentReservation>, IReadOnlyList<FulfillmentReservation>> selectReservations,
            CancellationToken cancellationToken)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var reservations = await _reservations.ListByOrderAsync(orderId, cancellationToken);
            var targets = selectReservations(reservations);
            var operations = await PlanAsync(order, targets, cancellationToken);
            var failures = new Dictionary<long, ProviderFailure?>();

            foreach (var operation in operations)
                failures[operation.Reservation.Id] = await ExecuteAsync(order, reservations, operation, cancellationToken);

            return new ConfirmResult(
                order.Id,
                order.Status,
                targets.Select(reservation => ResultOf(reservation, failures.GetValueOrDefault(reservation.Id))).ToList());
        }

        private async Task<IReadOnlyList<ConfirmationOperation>> PlanAsync(
            Order order,
            IReadOnlyList<FulfillmentReservation> targets,
            CancellationToken cancellationToken)
        {
            var now = _clock.GetDateTime();
            var operations = new List<ConfirmationOperation>();

            foreach (var reservation in targets.Where(item => item.AwaitsConfirmation))
            {
                var latest = await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.ConfirmInventory, cancellationToken);
                var unresolved = latest is { IsResumable: true } ? latest : null;

                if (!IsDispatchable(reservation, unresolved, _providers.CapabilityOf(order, reservation)))
                    continue;

                var provider = _providers.Resolve(reservation.FulfillmentProviderKey);

                reservation.EnsureConfirmableAt(now, order.LastTicketingDate);

                if (reservation.ValidationIsStaleAt(now))
                    await RenewValidationAsync(order, reservation, provider, cancellationToken);

                reservation.EnsureValidationCurrentAt(now);

                operations.Add(new ConfirmationOperation(provider, reservation, unresolved));
            }

            return operations;
        }

        private async Task<ProviderFailure?> ExecuteAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            ConfirmationOperation operation,
            CancellationToken cancellationToken)
        {
            var reservation = operation.Reservation;
            var startedAt = _clock.GetDateTime();

            reservation.EnsureConfirmableAt(startedAt, order.LastTicketingDate);
            reservation.EnsureValidationCurrentAt(startedAt);

            var task = operation.UnresolvedTask ?? await NewConfirmTaskAsync(reservation, startedAt, cancellationToken);
            var request = task.OriginalRequest(ProviderInteractionType.ConfirmHold)
                          ?? operation.Provider.ConfirmRequestFor(reservation.PrepareConfirmation());

            task.StartAttempt(_idGenerator, startedAt);
            task.RecordRequest(request, _idGenerator, startedAt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var outcome = await operation.Provider.ConfirmAsync(request, cancellationToken);
            var observedAt = _clock.GetDateTime();

            task.RecordResponse(outcome.OperationOutcome, reservation.ProviderOperationRef, outcome.Failure, outcome.Response, observedAt);
            reservation.RecordConfirmation(outcome, observedAt);
            task.CompleteAttempt(AttemptOutcomeOf(reservation), outcome.Failure, observedAt);

            await _summarizer.SummarizeAsync(order, reservations, cancellationToken);
            await _synchronizer.ProjectReservationChangedAsync(order.ToReadModelSnapshot(observedAt), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return outcome.Failure;
        }

        private static async Task RenewValidationAsync(
            Order order,
            FulfillmentReservation reservation,
            IReservationProvider provider,
            CancellationToken cancellationToken)
        {
            var coveredServiceIds = reservation.CoveredOrderServiceIds.ToHashSet();
            var units = provider.PlanUnits(order, order.Services.Where(service => coveredServiceIds.Contains(service.Id)).ToList());

            reservation.EnsurePlannedAs(units);

            var preparation = await provider.PrepareAsync(order, units, cancellationToken);

            reservation.RenewValidation(preparation.ValidationTimeLimit);
        }

        private async Task<FulfillmentTask> NewConfirmTaskAsync(
            FulfillmentReservation reservation,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken)
        {
            var taskId = _idGenerator.NewId();

            var task = FulfillmentTask.Create(
                taskId,
                reservation.OrderId,
                reservation.Id,
                OrderFulfillmentTaskType.ConfirmInventory,
                reservation.FulfillmentProviderKey,
                $"confirm-hold:{taskId}",
                reservation.CorrelationReference,
                reservation.Units.Select(unit => unit.Id).ToList(),
                OrderFulfillmentTargetAction.Confirm,
                _idGenerator,
                createdAt);

            await _tasks.AddAsync(task, cancellationToken);

            return task;
        }

        private static IReadOnlyList<FulfillmentReservation> ReservationsInScope(IReadOnlyList<FulfillmentReservation> reservations)
            => reservations.Where(IsInConfirmationScope).ToList();

        private static IReadOnlyList<FulfillmentReservation> RequestedReservations(
            long orderId,
            IReadOnlyList<FulfillmentReservation> reservations,
            IReadOnlyCollection<long> reservationIds)
        {
            var reservationsById = reservations.ToDictionary(reservation => reservation.Id);
            var requested = new List<FulfillmentReservation>();

            foreach (var reservationId in reservationIds.Distinct())
            {
                if (!reservationsById.TryGetValue(reservationId, out var reservation))
                    throw ExceptionFactory.ReservationNotFound(reservationId, orderId);

                if (!IsInConfirmationScope(reservation))
                    throw ExceptionFactory.ReservationIsNotConfirmable(reservation.Id, reservation.Status);

                requested.Add(reservation);
            }

            return requested;
        }

        private static bool IsInConfirmationScope(FulfillmentReservation reservation)
            => reservation.Status == FulfillmentReservationStatus.Confirmed || reservation.AwaitsConfirmation;

        private static bool IsDispatchable(
            FulfillmentReservation reservation,
            FulfillmentTask? unresolvedTask,
            ReservationCapability capability)
            => unresolvedTask is null
                ? reservation.Status == FulfillmentReservationStatus.Held
                : capability.SupportsSafeConfirmReplay;

        private static FulfillmentAttemptOutcome AttemptOutcomeOf(FulfillmentReservation reservation) => reservation.Status switch
        {
            FulfillmentReservationStatus.Confirmed => FulfillmentAttemptOutcome.Succeeded,
            FulfillmentReservationStatus.Unknown => FulfillmentAttemptOutcome.Unknown,
            _ => FulfillmentAttemptOutcome.Failed
        };

        private static ReservationConfirmationResult ResultOf(FulfillmentReservation reservation, ProviderFailure? failure)
            => new(
                reservation.Id,
                reservation.FulfillmentProviderKey,
                reservation.Status,
                failure?.Reason,
                failure?.Message);

        private sealed record ConfirmationOperation(
            IReservationProvider Provider,
            FulfillmentReservation Reservation,
            FulfillmentTask? UnresolvedTask);
    }
}
