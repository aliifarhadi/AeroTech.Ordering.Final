using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class ReservationReleaser : IReservationReleaser
    {
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IReservationProviderResolver _providers;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public ReservationReleaser(
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

        public async Task<ReleaseOutcome> ReleaseAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default)
        {
            var intent = reservation.PrepareRelease();
            var provider = _providers.Resolve(intent.ProviderKey);
            var task = await ReleaseTaskAsync(reservation, intent, cancellationToken);
            var request = task.OriginalRequest(ProviderInteractionType.ReleaseHold) ?? provider.ReleaseRequestFor(intent);
            var startedAt = _clock.GetDateTime();

            task.StartAttempt(_idGenerator, startedAt);
            task.RecordRequest(request, _idGenerator, startedAt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var outcome = await provider.ReleaseAsync(request, cancellationToken);
            var observedAt = _clock.GetDateTime();

            task.RecordResponse(outcome.OperationOutcome, intent.ProviderOperationRef, outcome.Failure, outcome.Response, observedAt);
            task.CompleteAttempt(AttemptOutcomeOf(outcome), outcome.Failure, observedAt);

            if (outcome.OperationOutcome == ProviderOperationOutcome.Succeeded)
                reservation.RecordReleased(observedAt);

            return outcome;
        }

        public async Task<bool> ReleaseWasRejectedAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default)
            => await _tasks.FindLatestAsync(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved, cancellationToken)
                is { Status: OrderFulfillmentStatus.Failed };

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
