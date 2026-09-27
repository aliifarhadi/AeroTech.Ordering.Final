using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Projection;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public sealed class VoidElectronicTicketsService : IVoidElectronicTicketsService
    {
        private readonly IOrderRepository _orders;
        private readonly IFulfillmentReservationRepository _reservations;
        private readonly IFulfillmentTaskRepository _tasks;
        private readonly IElectronicTicketRepository _tickets;
        private readonly IOrderReservationSummarizer _summarizer;
        private readonly IReservationLock _reservationLock;
        private readonly IOrderQueryDbSynchronizer _orderSynchronizer;
        private readonly IElectronicTicketQueryDbSynchronizer _ticketSynchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public VoidElectronicTicketsService(
            IOrderRepository orders,
            IFulfillmentReservationRepository reservations,
            IFulfillmentTaskRepository tasks,
            IElectronicTicketRepository tickets,
            IOrderReservationSummarizer summarizer,
            IReservationLock reservationLock,
            IOrderQueryDbSynchronizer orderSynchronizer,
            IElectronicTicketQueryDbSynchronizer ticketSynchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _orders = orders;
            _reservations = reservations;
            _tasks = tasks;
            _tickets = tickets;
            _summarizer = summarizer;
            _reservationLock = reservationLock;
            _orderSynchronizer = orderSynchronizer;
            _ticketSynchronizer = ticketSynchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public async Task<VoidElectronicTicketsResult> VoidAsync(
            long orderId,
            IReadOnlyList<ElectronicTicketVoidTarget> tickets,
            VoidReason reason,
            string? reasonDetail,
            IOrderAuthorization authorization,
            long actorId,
            long? callerOfficeId,
            CancellationToken cancellationToken = default)
        {
            await using var reservationLock = await _reservationLock.AcquireAsync(orderId, cancellationToken);

            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var orderTickets = await _tickets.ListByOrderAsync(orderId, cancellationToken);
            var requested = RequestedTickets(order, orderTickets, tickets);
            var voidedAt = _clock.GetDateTime();
            var voiding = requested.Where(target => !target.Ticket.IsVoided).ToList();

            foreach (var (ticket, expectedDocumentVersion) in voiding)
            {
                if (ticket.DocumentVersion != expectedDocumentVersion)
                    throw ExceptionFactory.ElectronicTicketVersionConflict(ticket.Id, ticket.DocumentVersion, expectedDocumentVersion);

                ticket.EnsureVoidableBy(callerOfficeId, voidedAt);
            }

            if (voiding.Count == 0)
                return ResultOf(order, null, requested);

            var task = NewVoidTask(order, voiding.Select(target => target.Ticket).ToList(), voidedAt);
            var reasonText = string.IsNullOrWhiteSpace(reasonDetail) ? null : reasonDetail.Trim();

            foreach (var (ticket, _) in voiding)
                ticket.Void(new DocumentVoidRecord(task.Id, reason.ToString(), reasonText, null, actorId, voidedAt), reason, actorId, callerOfficeId, _idGenerator);

            task.AddTargets(
                FulfillmentTargetKind.TicketCoupon,
                voiding.SelectMany(target => target.Ticket.Coupons).Select(coupon => coupon.Id).ToList(),
                OrderFulfillmentTargetAction.Void,
                _idGenerator);
            task.CompleteAttempt(FulfillmentAttemptOutcome.Succeeded, null, voidedAt);

            await _tasks.AddAsync(task, cancellationToken);

            _summarizer.SummarizeServicing(order, await _reservations.ListByOrderAsync(orderId, cancellationToken), orderTickets);

            await _orderSynchronizer.ProjectServicedAsync(order.ToReadModelSnapshot(voidedAt), cancellationToken);
            await _ticketSynchronizer.ProjectVoidedAsync(voiding.Select(target => target.Ticket.ToReadModelSnapshot()).ToList(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ResultOf(order, task.Id, requested);
        }

        private FulfillmentTask NewVoidTask(Order order, IReadOnlyCollection<ElectronicTicket> tickets, DateTimeOffset createdAt)
        {
            var taskId = _idGenerator.NewId();

            var task = FulfillmentTask.Create(
                taskId,
                order.Id,
                null,
                OrderFulfillmentTaskType.VoidTicket,
                FulfillmentProviderKeys.LocalDocumentAuthority,
                $"void-ticket:{taskId}",
                $"order:{order.Id}:void:{taskId}",
                FulfillmentTargetKind.ElectronicTicket,
                tickets.Select(ticket => ticket.Id).ToList(),
                OrderFulfillmentTargetAction.Void,
                _idGenerator,
                createdAt);

            task.StartAttempt(_idGenerator, createdAt);

            return task;
        }

        private static IReadOnlyList<RequestedTicket> RequestedTickets(
            Order order,
            IReadOnlyCollection<ElectronicTicket> orderTickets,
            IReadOnlyList<ElectronicTicketVoidTarget> targets)
        {
            var ticketsById = orderTickets.ToDictionary(ticket => ticket.Id);

            return targets
                .Select(target => ticketsById.TryGetValue(target.ElectronicTicketId, out var ticket)
                    ? new RequestedTicket(ticket, target.ExpectedDocumentVersion)
                    : throw ExceptionFactory.ElectronicTicketNotFoundInOrder(target.ElectronicTicketId, order.Id))
                .ToList();
        }

        private static VoidElectronicTicketsResult ResultOf(Order order, long? voidFulfillmentTaskId, IReadOnlyList<RequestedTicket> requested)
            => new(
                order.Id,
                order.Status,
                voidFulfillmentTaskId,
                requested
                    .Select(target => target.Ticket)
                    .Select(ticket => new VoidedTicketResult(
                        ticket.Id,
                        ticket.DocumentNumber,
                        ticket.StatusSummary,
                        ticket.DocumentVersion,
                        ticket.VoidRecord?.VoidFulfillmentTaskId,
                        ticket.VoidRecord?.VoidedAt,
                        ticket.Coupons
                            .OrderBy(coupon => coupon.CouponNumber)
                            .Select(coupon => new VoidedCouponResult(coupon.Id, coupon.CouponNumber, coupon.FinancialStatus))
                            .ToList()))
                    .ToList());

        private sealed record RequestedTicket(ElectronicTicket Ticket, int ExpectedDocumentVersion);
    }
}
