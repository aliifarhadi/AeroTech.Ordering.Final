using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.DocumentStockAggregate.Projection;
using AeroTech.Ordering.Application.DocumentStockAggregate.Services;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Projection;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application.OrderAggregate.Services.Issuance;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder
{
    public sealed class IssueOrderService : IIssueOrderService
    {
        private static readonly IReadOnlyCollection<OrderFulfillmentTaskType> ConflictingTaskTypes =
        [
            OrderFulfillmentTaskType.ReserveInventory,
            OrderFulfillmentTaskType.ConfirmInventory,
            OrderFulfillmentTaskType.ReleaseReserved,
            OrderFulfillmentTaskType.CancelConfirmed,
            OrderFulfillmentTaskType.IssueTicket
        ];

        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IElectronicTicketRepository _tickets;
        private readonly IDocumentStockRepository _stocks;
        private readonly IReservationProviderResolver _providers;
        private readonly IIssuancePlanner _planner;
        private readonly IReservationLock _reservationLock;
        private readonly IDocumentStockLock _stockLock;
        private readonly IOrderQueryDbSynchronizer _orderSynchronizer;
        private readonly IElectronicTicketQueryDbSynchronizer _ticketSynchronizer;
        private readonly IDocumentStockQueryDbSynchronizer _stockSynchronizer;
        private readonly IAirlineOfficeTimeZoneResolver _officeTimeZones;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public IssueOrderService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IFulfillmentTaskRepository tasks,
            IElectronicTicketRepository tickets,
            IDocumentStockRepository stocks,
            IReservationProviderResolver providers,
            IIssuancePlanner planner,
            IReservationLock reservationLock,
            IDocumentStockLock stockLock,
            IOrderQueryDbSynchronizer orderSynchronizer,
            IElectronicTicketQueryDbSynchronizer ticketSynchronizer,
            IDocumentStockQueryDbSynchronizer stockSynchronizer,
            IAirlineOfficeTimeZoneResolver officeTimeZones,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _tasks = tasks;
            _tickets = tickets;
            _stocks = stocks;
            _providers = providers;
            _planner = planner;
            _reservationLock = reservationLock;
            _stockLock = stockLock;
            _orderSynchronizer = orderSynchronizer;
            _ticketSynchronizer = ticketSynchronizer;
            _stockSynchronizer = stockSynchronizer;
            _officeTimeZones = officeTimeZones;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<IssueOrderResult> IssueAsync(
            long orderId,
            long ticketDocumentStockId,
            IOrderAuthorization authorization,
            long? issuedByActorId,
            CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var tickets = await _tickets.ListByOrderAsync(orderId, cancellationToken);
            var services = _planner.OutstandingServices(order, tickets);

            if (services.Count == 0)
                return tickets.Count == 0
                    ? throw ExceptionFactory.OrderHasNoTicketableServices(order.Id)
                    : ResultOf(order, tickets, tickets[^1].IssueFulfillmentTaskId);

            order.EnsureIssuableAt(_clock.GetDateTime());

            var reservations = await _reservations.ListByOrderAsync(orderId, cancellationToken);
            var scope = _planner.ScopeOf(services, reservations);

            if (await _tasks.AnyUnresolvedAsync(order.Id, scope.Reservations.Select(reservation => reservation.Id).ToList(), ConflictingTaskTypes, cancellationToken))
                throw ExceptionFactory.OrderHasUnresolvedFulfillmentEffect(order.Id);

            var plans = _planner.PlanTickets(order, scope);
            var stock = await _stocks.GetAsync(ticketDocumentStockId, cancellationToken)
                        ?? throw ExceptionFactory.DocumentStockNotFound(ticketDocumentStockId);

            stock.EnsureCanIssue(AccountableDocumentKind.ElectronicTicket, plans.Count);

            await RenewStaleValidationAsync(order, scope.Reservations, cancellationToken);

            await using var stockLock = await _stockLock.AcquireAsync(stock.Id, cancellationToken);
            await _stocks.ReloadAsync(stock, cancellationToken);

            var issuedAt = _clock.GetDateTime();

            order.EnsureIssuableAt(issuedAt);
            EnsureValidationCurrentAt(scope.Reservations, issuedAt);
            stock.EnsureCanIssue(AccountableDocumentKind.ElectronicTicket, plans.Count);

            var task = NewIssueTask(order, scope, issuedAt);
            var allocations = plans.Select(plan => stock.Allocate(task.Id, plan.DocumentRole, _idGenerator, issuedAt)).ToList();

            if (await _tickets.AnyWithDocumentNumberAsync(allocations.Select(allocation => allocation.DocumentNumber).ToList(), cancellationToken))
                throw ExceptionFactory.DocumentNumberIsAlreadyIssued(string.Join(", ", allocations.Select(allocation => allocation.DocumentNumber)));

            var voidDeadline = await VoidDeadlineOfAsync(stock, issuedAt, cancellationToken);
            var issued = plans
                .Zip(allocations, (plan, allocation) => ElectronicTicket.IssueLocally(
                    _idGenerator.NewId(),
                    new IssueElectronicTicketArgs(
                        order.Id,
                        plan.TravellerId,
                        plan.TravellerProfileRevisionId,
                        task.Id,
                        allocation.DocumentNumber,
                        IssuanceContextOf(order, stock, issuedByActorId),
                        voidDeadline,
                        order.CurrencyId,
                        plan.Coupons),
                    _idGenerator,
                    issuedAt))
                .ToList();

            allocations.ForEach(allocation => stock.MarkIssued(allocation.Id, issuedAt));

            task.AddTargets(FulfillmentTargetKind.DocumentStockAllocation, allocations.Select(allocation => allocation.Id).ToList(), OrderFulfillmentTargetAction.Issue, _idGenerator);
            task.AddTargets(FulfillmentTargetKind.ElectronicTicket, issued.Select(ticket => ticket.Id).ToList(), OrderFulfillmentTargetAction.Issue, _idGenerator);
            task.AddTargets(FulfillmentTargetKind.TicketCoupon, issued.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.Id).ToList(), OrderFulfillmentTargetAction.Issue, _idGenerator);
            task.CompleteAttempt(FulfillmentAttemptOutcome.Succeeded, null, issuedAt);

            IReadOnlyList<ElectronicTicket> orderTickets = [.. tickets, .. issued];

            order.MarkTicketed(orderTickets.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.CurrentOrderServiceId).ToHashSet());

            await _tasks.AddAsync(task, cancellationToken);

            foreach (var ticket in issued)
                await _tickets.AddAsync(ticket, cancellationToken);

            await _orderSynchronizer.ProjectIssuedAsync(order.ToReadModelSnapshot(issuedAt), cancellationToken);
            await _ticketSynchronizer.ProjectIssuedAsync(issued.Select(ticket => ticket.ToReadModelSnapshot()).ToList(), cancellationToken);
            await _stockSynchronizer.ProjectAsync(stock.ToReadModelSnapshot(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResultOf(order, orderTickets, task.Id);
        }

        private async Task RenewStaleValidationAsync(
            Order order,
            IEnumerable<FulfillmentReservation> reservations,
            CancellationToken cancellationToken)
        {
            var now = _clock.GetDateTime();

            foreach (var reservation in reservations.Where(reservation => reservation.ValidationIsStaleAt(now)))
            {
                var provider = _providers.Resolve(reservation.FulfillmentProviderKey);
                var coveredServiceIds = reservation.CoveredOrderServiceIds.ToHashSet();
                var units = provider.PlanUnits(order, order.Services.Where(service => coveredServiceIds.Contains(service.Id)).ToList());

                reservation.EnsurePlannedAs(units);

                var preparation = await provider.PrepareAsync(order, units, cancellationToken);

                reservation.RenewIssueValidation(preparation.ValidationTimeLimit);
            }
        }

        private static void EnsureValidationCurrentAt(IEnumerable<FulfillmentReservation> reservations, DateTimeOffset now)
        {
            if (reservations.FirstOrDefault(reservation => reservation.ValidationIsStaleAt(now)) is { } stale)
                throw ExceptionFactory.ReservationValidationIsStaleForIssue(stale.Id, stale.ReservationValidationTimeLimit);
        }

        private FulfillmentTask NewIssueTask(Order order, IssuanceScope scope, DateTimeOffset createdAt)
        {
            var taskId = _idGenerator.NewId();

            var task = FulfillmentTask.Create(
                taskId,
                order.Id,
                null,
                OrderFulfillmentTaskType.IssueTicket,
                FulfillmentProviderKeys.LocalDocumentAuthority,
                $"issue-ticket:{taskId}",
                $"order:{order.Id}:issue:{taskId}",
                FulfillmentTargetKind.OrderService,
                scope.Services.Select(service => service.Id).ToList(),
                OrderFulfillmentTargetAction.Issue,
                _idGenerator,
                createdAt);

            task.StartAttempt(_idGenerator, createdAt);

            return task;
        }

        private async Task<DateTimeOffset?> VoidDeadlineOfAsync(DocumentStock stock, DateTimeOffset issuedAt, CancellationToken cancellationToken)
            => stock.OfficeId is { } officeId
                ? LocalVoidPolicy.DeadlineFor(issuedAt, await _officeTimeZones.FindTimeZoneIdAsync(officeId, cancellationToken))
                : null;

        private static DocumentIssuanceContext IssuanceContextOf(Order order, DocumentStock stock, long? issuedByActorId)
            => new(
                stock.OwnerAirlineId,
                null,
                stock.OfficeId,
                issuedByActorId,
                order.SalesContext.TravelAgencyId,
                null,
                null,
                order.SalesContext.Channel,
                null);

        private static IssueOrderResult ResultOf(Order order, IReadOnlyList<ElectronicTicket> tickets, long issueFulfillmentTaskId)
            => new(
                order.Id,
                order.Status,
                issueFulfillmentTaskId,
                tickets
                    .Select(ticket => new IssuedTicketResult(
                        ticket.Id,
                        ticket.TravellerId,
                        ticket.DocumentNumber,
                        ticket.StatusSummary,
                        ticket.IssuedAt,
                        ticket.IssuedTotal,
                        ticket.CurrencyId,
                        ticket.Coupons
                            .OrderBy(coupon => coupon.CouponNumber)
                            .Select(coupon => new IssuedCouponResult(
                                coupon.Id,
                                coupon.CouponNumber,
                                coupon.CurrentOrderServiceId,
                                coupon.OrderSegmentId,
                                coupon.FinancialStatus,
                                coupon.ControlStatus,
                                coupon.IssuanceValue))
                            .ToList()))
                    .ToList());
    }
}
