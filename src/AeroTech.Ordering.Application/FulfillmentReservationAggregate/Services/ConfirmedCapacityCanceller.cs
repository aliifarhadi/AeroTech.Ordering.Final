using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class ConfirmedCapacityCanceller : IConfirmedCapacityCanceller
    {
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IReservationProviderResolver _providers;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public ConfirmedCapacityCanceller(
            IFulfillmentTaskRepository tasks,
            IReservationProviderResolver providers,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _tasks = tasks;
            _providers = providers;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<IReadOnlyDictionary<long, IReadOnlySet<long>>> UnresolvedTargetsAsync(
            IReadOnlyCollection<FulfillmentReservation> reservations,
            CancellationToken cancellationToken = default)
        {
            var unresolvedTargets = new Dictionary<long, IReadOnlySet<long>>();

            foreach (var reservation in reservations)
                if (await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed, cancellationToken) is { IsResumable: true } unresolvedTask)
                    unresolvedTargets[reservation.Id] = ReservationUnitTargetsOf(unresolvedTask);

            return unresolvedTargets;
        }

        public async Task<ConfirmedCancellationOutcome?> ResumeAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default)
        {
            var unresolvedTask = await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed, cancellationToken);

            if (unresolvedTask is not { IsResumable: true })
                return null;

            var request = unresolvedTask.OriginalRequest(ProviderInteractionType.CancelConfirmed)
                          ?? throw ExceptionFactory.ConfirmedCancellationRecoveryRequestIsMissing(unresolvedTask.Id, reservation.Id);

            return await DispatchAsync(reservation, unresolvedTask, request, isRecovery: true, cancellationToken);
        }

        public async Task<ConfirmedCancellationOutcome> CancelAsync(
            FulfillmentReservation reservation,
            IReadOnlyCollection<long> unitIds,
            string commercialReason,
            CancellationToken cancellationToken = default)
        {
            var intent = reservation.PrepareConfirmedCancellation(unitIds, commercialReason);
            var request = _providers.Resolve(intent.ProviderKey).CancelConfirmedRequestFor(intent);
            var task = NewCancellationTask(reservation, intent);

            await _tasks.AddAsync(task, cancellationToken);

            return await DispatchAsync(reservation, task, request, isRecovery: false, cancellationToken);
        }

        private async Task<ConfirmedCancellationOutcome> DispatchAsync(
            FulfillmentReservation reservation,
            FulfillmentTask task,
            ProviderRequest request,
            bool isRecovery,
            CancellationToken cancellationToken)
        {
            var provider = _providers.Resolve(task.FulfillmentProviderKey);
            var targetUnitIds = ReservationUnitTargetsOf(task);
            var startedAt = _clock.GetDateTime();

            task.StartAttempt(_idGenerator, startedAt);
            task.RecordRequest(request, _idGenerator, startedAt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var providerOutcome = await provider.CancelConfirmedAsync(request, cancellationToken);
            var outcome = isRecovery ? ReplayOutcomeOf(providerOutcome) : providerOutcome;
            var observedAt = _clock.GetDateTime();

            task.RecordResponse(outcome.OperationOutcome, reservation.ProviderOperationRef, outcome.Failure, outcome.Response, observedAt);
            reservation.RecordConfirmedCancellation(targetUnitIds, outcome, observedAt);
            task.CompleteAttempt(AttemptOutcomeOf(outcome), outcome.Failure, observedAt);

            return outcome;
        }

        private FulfillmentTask NewCancellationTask(FulfillmentReservation reservation, ConfirmedCancellationIntent intent)
        {
            var taskId = _idGenerator.NewId();

            return FulfillmentTask.Create(
                taskId,
                reservation.OrderId,
                reservation.Id,
                OrderFulfillmentTaskType.CancelConfirmed,
                reservation.FulfillmentProviderKey,
                $"cancel-confirmed:{taskId}",
                $"order:{reservation.OrderId}:reservation:{reservation.Id}:cancel:{taskId}",
                FulfillmentTargetKind.ReservationUnit,
                intent.Units.Select(unit => unit.ReservationUnitId).ToList(),
                OrderFulfillmentTargetAction.Cancel,
                _idGenerator,
                _clock.GetDateTime());
        }

        private static IReadOnlySet<long> ReservationUnitTargetsOf(FulfillmentTask task)
            => task.Targets.Count > 0 && task.Targets.All(target => target.TargetKind == FulfillmentTargetKind.ReservationUnit)
                ? task.Targets.Select(target => target.TargetId).ToHashSet()
                : throw ExceptionFactory.ConfirmedCancellationTaskTargetsAreInvalid(task.Id);

        private static ConfirmedCancellationOutcome ReplayOutcomeOf(ConfirmedCancellationOutcome outcome)
            => outcome is { OperationOutcome: ProviderOperationOutcome.Rejected, ObservedStatus: null }
                ? outcome with { OperationOutcome = ProviderOperationOutcome.Unknown }
                : outcome;

        private static FulfillmentAttemptOutcome AttemptOutcomeOf(ConfirmedCancellationOutcome outcome) => outcome.OperationOutcome switch
        {
            ProviderOperationOutcome.Succeeded => FulfillmentAttemptOutcome.Succeeded,
            ProviderOperationOutcome.Rejected => FulfillmentAttemptOutcome.Failed,
            _ => FulfillmentAttemptOutcome.Unknown
        };
    }
}
