using System.Diagnostics;
using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder
{
    public sealed class CancelOrderService : ICancelOrderService
    {
        private static readonly IReadOnlyCollection<OrderFulfillmentTaskType> CompetingTaskTypes =
        [
            OrderFulfillmentTaskType.ReserveInventory,
            OrderFulfillmentTaskType.ConfirmInventory,
            OrderFulfillmentTaskType.IssueTicket,
            OrderFulfillmentTaskType.VoidTicket
        ];

        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IElectronicTicketRepository _tickets;
        private readonly ICancellationPlanner _planner;
        private readonly IReservationReleaser _releaser;
        private readonly IConfirmedCapacityCanceller _canceller;
        private readonly IOrderReservationSummarizer _summarizer;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public CancelOrderService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IFulfillmentTaskRepository tasks,
            IElectronicTicketRepository tickets,
            ICancellationPlanner planner,
            IReservationReleaser releaser,
            IConfirmedCapacityCanceller canceller,
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
            _tickets = tickets;
            _planner = planner;
            _releaser = releaser;
            _canceller = canceller;
            _summarizer = summarizer;
            _reservationLock = reservationLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<CancelOrderResult> CancelAsync(
            long orderId,
            IReadOnlyCollection<long> serviceIds,
            VoidReason reason,
            string? reasonText,
            IOrderAuthorization authorization,
            SalesContext actorContext,
            CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var endingServices = order.CancellationScopeOf(serviceIds);

            if (endingServices.Count == 0)
                return ResultOf(order, null, [], []);

            var tickets = await _tickets.ListByOrderAsync(orderId, cancellationToken);

            _planner.EnsureNoActiveDocumentCovers(endingServices, tickets);

            var reservations = await _reservations.ListByOrderAsync(orderId, cancellationToken);
            var overlapping = OverlappingReservations(reservations, endingServices);

            if (await _tasks.AnyUnresolvedAsync(order.Id, overlapping.Select(reservation => reservation.Id).ToList(), CompetingTaskTypes, cancellationToken))
                throw ExceptionFactory.OrderHasUnresolvedFulfillmentEffect(order.Id);

            var reasonCode = reason.ToString();
            var settlements = _planner.PlanSettlements(
                order,
                endingServices,
                overlapping,
                await _canceller.UnresolvedTargetsAsync(overlapping, cancellationToken));
            var failures = new Dictionary<long, ProviderFailure?>();

            foreach (var settlement in settlements)
                failures[settlement.Reservation.Id] = await SettleAsync(settlement, reasonCode, cancellationToken);

            var committedAt = _clock.GetDateTime();
            var change = failures.Values.All(failure => failure is null) && _planner.IsSettled(order, endingServices, overlapping)
                ? order.Cancel(endingServices.Select(service => service.Id).ToList(), actorContext, reasonCode, reasonText, _idGenerator, committedAt)
                : null;

            _summarizer.SummarizeServicing(order, reservations, tickets);
            await _synchronizer.ProjectServicedAsync(order.ToReadModelSnapshot(committedAt), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResultOf(
                order,
                change,
                change is null ? [] : endingServices.Select(service => service.Id).ToList(),
                settlements.Select(settlement => ResultOf(settlement.Reservation, failures[settlement.Reservation.Id])).ToList());
        }

        private Task<ProviderFailure?> SettleAsync(ReservationSettlement settlement, string reasonCode, CancellationToken cancellationToken)
            => settlement switch
            {
                HeldRelease release => ReleaseAsync(release.Reservation, cancellationToken),
                ConfirmedCancellation cancellation => CancelConfirmedAsync(cancellation.Reservation, cancellation.UnitIds, reasonCode, cancellationToken),
                CancellationRecovery recovery => RecoverAsync(recovery, reasonCode, cancellationToken),
                _ => throw new UnreachableException()
            };

        private async Task<ProviderFailure?> ReleaseAsync(FulfillmentReservation reservation, CancellationToken cancellationToken)
            => (await _releaser.ReleaseAsync(reservation, cancellationToken)).Failure;

        private async Task<ProviderFailure?> CancelConfirmedAsync(
            FulfillmentReservation reservation,
            IReadOnlyCollection<long> unitIds,
            string reasonCode,
            CancellationToken cancellationToken)
            => (await _canceller.CancelAsync(reservation, unitIds, reasonCode, cancellationToken)).Failure;

        private async Task<ProviderFailure?> RecoverAsync(CancellationRecovery recovery, string reasonCode, CancellationToken cancellationToken)
        {
            var outcome = await _canceller.ResumeAsync(recovery.Reservation, cancellationToken);

            if (outcome is not { OperationOutcome: ProviderOperationOutcome.Succeeded })
                return outcome?.Failure;

            var stillConfirmed = recovery.Reservation.Units
                .Where(unit => recovery.UnitIds.Contains(unit.Id) && unit.Status == ReservationMemberStatus.Confirmed)
                .Select(unit => unit.Id)
                .ToList();

            return stillConfirmed.Count == 0
                ? null
                : await CancelConfirmedAsync(recovery.Reservation, stillConfirmed, reasonCode, cancellationToken);
        }

        private static IReadOnlyList<FulfillmentReservation> OverlappingReservations(
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlyCollection<OrderService> endingServices)
        {
            var endingServiceIds = endingServices.Select(service => service.Id).ToHashSet();

            return reservations
                .Where(reservation => reservation.CoveredOrderServiceIds.Any(endingServiceIds.Contains))
                .ToList();
        }

        private static CancelOrderResult ResultOf(
            Order order,
            OrderChange? change,
            IReadOnlyList<long> cancelledServiceIds,
            IReadOnlyList<CancellationReservationResult> reservations)
            => new(
                order.Id,
                order.Status,
                order.CommercialVersion,
                change?.Id,
                cancelledServiceIds,
                reservations);

        private static CancellationReservationResult ResultOf(FulfillmentReservation reservation, ProviderFailure? failure)
            => new(
                reservation.Id,
                reservation.FulfillmentProviderKey,
                reservation.Status,
                failure?.Reason,
                failure?.Message);
    }
}
