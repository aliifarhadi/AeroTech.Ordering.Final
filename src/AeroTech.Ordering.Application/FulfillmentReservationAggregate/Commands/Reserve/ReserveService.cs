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
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public sealed class ReserveService : IReserveService
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

        public ReserveService(
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

        public Task<ReserveResult> ReserveOrderAsync(
            long orderId,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => ReserveAsync(orderId, authorization, RequiredServices, cancellationToken);

        public Task<ReserveResult> ReserveServicesAsync(
            long orderId,
            IReadOnlyCollection<long> orderServiceIds,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => ReserveAsync(orderId, authorization, order => RequestedServices(order, orderServiceIds), cancellationToken);

        private async Task<ReserveResult> ReserveAsync(
            long orderId,
            IOrderAuthorization authorization,
            Func<Order, IReadOnlyList<OrderService>> selectServices,
            CancellationToken cancellationToken)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);
            order.EnsureReservable();

            var services = selectServices(order);
            var reservations = (await _reservations.ListByOrderAsync(orderId, cancellationToken)).ToList();

            var resumed = await ResumeAsync(order, reservations, services, cancellationToken);
            var started = await PlanAsync(order, reservations, services, cancellationToken);
            var operations = resumed.Concat(started).ToList();

            if (operations.Count == 0)
                return new ReserveResult(order.Id, order.Status, order.RecordLocator, []);

            reservations.AddRange(started.Select(operation => operation.Reservation));
            await StartAttemptsAsync(operations, cancellationToken);

            var results = new List<ReservationOperationResult>();

            foreach (var operation in operations)
                results.Add(await ExecuteAsync(order, reservations, operation, cancellationToken));

            return new ReserveResult(order.Id, order.Status, order.RecordLocator, results);
        }

        private async Task<IReadOnlyList<ReservationOperation>> ResumeAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlyCollection<OrderService> services,
            CancellationToken cancellationToken)
        {
            var serviceIds = services.Select(service => service.Id).ToHashSet();
            var operations = new List<ReservationOperation>();

            foreach (var reservation in reservations.Where(item => item.IsUnresolved && item.CoveredOrderServiceIds.Any(serviceIds.Contains)))
            {
                var provider = _providers.Resolve(reservation.FulfillmentProviderKey);
                var coveredServiceIds = reservation.CoveredOrderServiceIds.ToHashSet();
                var units = provider.PlanUnits(order, order.Services.Where(service => coveredServiceIds.Contains(service.Id)).ToList());

                reservation.EnsurePlannedAs(units);

                var task = await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.ReserveInventory, cancellationToken);

                if (task is not { IsResumable: true } || task.OriginalRequest(ProviderInteractionType.CreateHold) is not { } request)
                    throw ExceptionFactory.ReservationTaskIsNotResumable(reservation.Id);

                var readBack = reservation.ProviderOperationRef is { } providerOperationRef
                               && _providers.CapabilityOf(order, reservation).SupportsReadBack
                    ? provider.ReadRequestFor(providerOperationRef)
                    : null;

                operations.Add(new ReservationOperation(provider, reservation, task, IntentFor(reservation, units), request, readBack));
            }

            return operations;
        }

        private async Task<IReadOnlyList<ReservationOperation>> PlanAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlyCollection<OrderService> services,
            CancellationToken cancellationToken)
        {
            var latestUnitStatusByService = ReservationCoverage.LatestUnitStatusByService(reservations);
            var latestReservationByService = ReservationCoverage.LatestReservationByService(reservations);
            var operations = new List<ReservationOperation>();

            var groups = services
                .Where(service => IsUncovered(service.Id, latestUnitStatusByService))
                .GroupBy(service => (service.FulfillmentProviderKey, _providers.CapabilityOf(service).Mode))
                .ToList();

            if (groups.Count > 0)
                order.EnsureNewReservationAllowedAt(_clock.GetDateTime());

            foreach (var group in groups)
            {
                var provider = _providers.Resolve(group.Key.FulfillmentProviderKey);
                var units = provider.PlanUnits(order, group.ToList());

                EnsureReservableAnew(units, latestUnitStatusByService, latestReservationByService, _clock.GetDateTime());

                var preparation = await provider.PrepareAsync(order, units, cancellationToken);
                var createdAt = _clock.GetDateTime();

                var reservation = FulfillmentReservation.Create(
                    _idGenerator.NewId(),
                    order.Id,
                    provider.ProviderKey,
                    group.Key.Mode,
                    preparation.RequestedExpiresAt,
                    preparation.ValidationTimeLimit,
                    units,
                    _idGenerator,
                    createdAt);

                var intent = IntentFor(reservation, units);

                operations.Add(new ReservationOperation(
                    provider,
                    reservation,
                    NewReserveTask(reservation, createdAt),
                    intent,
                    provider.ReserveRequestFor(order, intent),
                    null));
            }

            foreach (var operation in operations)
            {
                await _reservations.AddAsync(operation.Reservation, cancellationToken);
                await _tasks.AddAsync(operation.Task, cancellationToken);
            }

            return operations;
        }

        private async Task StartAttemptsAsync(IReadOnlyCollection<ReservationOperation> operations, CancellationToken cancellationToken)
        {
            var startedAt = _clock.GetDateTime();

            foreach (var operation in operations)
            {
                operation.Task.StartAttempt(_idGenerator, startedAt);
                operation.Task.RecordRequest(operation.ReadBack ?? operation.Request, _idGenerator, startedAt);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<ReservationOperationResult> ExecuteAsync(
            Order order,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            ReservationOperation operation,
            CancellationToken cancellationToken)
        {
            var outcome = operation.ReadBack is { } readBack
                ? await CallAsync(operation, readBack, operation.Provider.ReadAsync, cancellationToken)
                : null;

            if (outcome is null or { OperationOutcome: ProviderOperationOutcome.Unknown })
            {
                if (operation.ReadBack is not null)
                {
                    operation.Task.RecordRequest(operation.Request, _idGenerator, _clock.GetDateTime());
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                outcome = await CallAsync(operation, operation.Request, operation.Provider.ReserveAsync, cancellationToken);
            }

            var observedAt = _clock.GetDateTime();
            var reservation = operation.Reservation;

            reservation.RecordOutcome(outcome, observedAt);
            operation.Task.CompleteAttempt(AttemptOutcomeOf(reservation), outcome.Failure, observedAt);

            await _summarizer.SummarizeAsync(order, reservations, cancellationToken);
            await _synchronizer.ProjectReservationChangedAsync(order.ToReadModelSnapshot(observedAt), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ReservationOperationResult(
                reservation.Id,
                reservation.FulfillmentProviderKey,
                reservation.Status,
                reservation.ProviderOperationRef,
                reservation.ExpiresAt,
                outcome.Failure?.Reason,
                outcome.Failure?.Message);
        }

        private async Task<ReservationOutcome> CallAsync(
            ReservationOperation operation,
            ProviderRequest request,
            Func<ReservationIntent, ProviderRequest, CancellationToken, Task<ReservationOutcome>> call,
            CancellationToken cancellationToken)
        {
            var outcome = await call(operation.Intent, request, cancellationToken);

            operation.Task.RecordResponse(
                outcome.OperationOutcome,
                outcome.ProviderOperationRef,
                outcome.Failure,
                outcome.Response,
                _clock.GetDateTime());

            return outcome;
        }

        private IReadOnlyList<OrderService> RequiredServices(Order order)
            => order.Services.Where(_providers.RequiresReservation).ToList();

        private IReadOnlyList<OrderService> RequestedServices(Order order, IReadOnlyCollection<long> orderServiceIds)
        {
            var servicesById = order.Services.ToDictionary(service => service.Id);
            var requested = new List<OrderService>();

            foreach (var serviceId in orderServiceIds.Distinct())
            {
                if (!servicesById.TryGetValue(serviceId, out var service))
                    throw ExceptionFactory.OrderServiceIsNotInOrder(serviceId, order.Id);

                if (service.CommercialStatus != OrderServiceCommercialState.Active)
                    throw ExceptionFactory.OrderServiceIsNotActive(serviceId);

                if (!_providers.RequiresReservation(service))
                    throw ExceptionFactory.OrderServiceDoesNotRequireReservation(serviceId);

                requested.Add(service);
            }

            return requested;
        }

        private FulfillmentTask NewReserveTask(FulfillmentReservation reservation, DateTimeOffset createdAt)
            => FulfillmentTask.Create(
                _idGenerator.NewId(),
                reservation.OrderId,
                reservation.Id,
                OrderFulfillmentTaskType.ReserveInventory,
                reservation.FulfillmentProviderKey,
                reservation.IdempotencyKey,
                reservation.CorrelationReference,
                reservation.Units.Select(unit => unit.Id).ToList(),
                OrderFulfillmentTargetAction.Reserve,
                _idGenerator,
                createdAt);

        private static ReservationIntent IntentFor(FulfillmentReservation reservation, IReadOnlyList<ReservationUnitIntent> units)
            => new(
                reservation.FulfillmentProviderKey,
                reservation.IdempotencyKey,
                reservation.CorrelationReference,
                reservation.RequestedExpiresAt,
                units);

        private static void EnsureReservableAnew(
            IReadOnlyList<ReservationUnitIntent> units,
            IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService,
            IReadOnlyDictionary<long, FulfillmentReservation> latestReservationByService,
            DateTimeOffset now)
        {
            foreach (var unit in units)
            {
                foreach (var serviceId in unit.OrderServiceIds)
                {
                    if (!IsUncovered(serviceId, latestUnitStatusByService))
                        throw ExceptionFactory.ReservationUnitIncludesCoveredService(unit.UnitCorrelationKey, serviceId);

                    if (latestReservationByService.TryGetValue(serviceId, out var previous))
                        previous.EnsureReplaceableAt(now);
                }
            }
        }

        private static bool IsUncovered(long serviceId, IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService)
            => !latestUnitStatusByService.TryGetValue(serviceId, out var status) || status.IsTerminalNegative();

        private static FulfillmentAttemptOutcome AttemptOutcomeOf(FulfillmentReservation reservation)
        {
            if (reservation.IsUnresolved)
                return FulfillmentAttemptOutcome.Unknown;

            return reservation.Status == FulfillmentReservationStatus.Rejected
                ? FulfillmentAttemptOutcome.Failed
                : FulfillmentAttemptOutcome.Succeeded;
        }

        private sealed record ReservationOperation(
            IReservationProvider Provider,
            FulfillmentReservation Reservation,
            FulfillmentTask Task,
            ReservationIntent Intent,
            ProviderRequest Request,
            ProviderRequest? ReadBack);
    }
}
