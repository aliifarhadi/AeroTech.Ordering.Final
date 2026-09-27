using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ElectronicTicketVoidTests
{
    private const string IssuingOfficeTimeZone = "Asia/Tehran";

    private static readonly DateTimeOffset TehranMidnight = new(2026, 10, 1, 20, 30, 0, TimeSpan.Zero);

    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task New_local_ticket_gets_the_next_local_midnight_of_its_issuing_office_as_void_deadline()
    {
        var (_, tickets) = await IssuedOrderAsync();

        Assert.Equal(TehranMidnight, tickets.Single().VoidDeadline);
        Assert.NotEqual(tickets.Single().IssuedAt.AddHours(24), tickets.Single().VoidDeadline);
    }

    [Fact]
    public async Task Ticket_from_a_stock_without_an_office_has_no_void_deadline()
    {
        var (_, tickets) = await IssuedOrderAsync(officeId: null);

        Assert.Null(tickets.Single().VoidDeadline);
    }

    [Fact]
    public async Task Ticket_of_an_office_without_a_time_zone_has_no_void_deadline()
    {
        var (_, tickets) = await IssuedOrderAsync(timeZoneId: null);

        Assert.Null(tickets.Single().VoidDeadline);
    }

    [Fact]
    public async Task Ticket_without_a_void_deadline_requires_a_refund()
    {
        var (order, tickets) = await IssuedOrderAsync(timeZoneId: null);

        await AssertRefundRequiredAsync(order, 2811, ReservationHarness.IssuingOfficeId, tickets.ToArray());
    }

    [Fact]
    public async Task Issuing_office_voids_before_the_deadline_with_exact_document_history()
    {
        var (order, tickets) = await IssuedOrderAsync();
        var ticket = tickets.Single();
        _harness.Clock.Now = TehranMidnight.AddMinutes(-1);

        var result = await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, ticket);

        var task = _harness.Tasks.Committed.Single(item => item.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.Equal((ElectronicTicketStatus.Voided, 2), (ticket.StatusSummary, ticket.DocumentVersion));
        Assert.All(ticket.Coupons, coupon => Assert.Equal(TicketCouponFinancialStatus.Void, coupon.FinancialStatus));
        Assert.Equal(
            (task.Id, nameof(VoidReason.CustomerRequest), "Passenger changed plans", (string?)null, (long?)ReservationHarness.IssueActorId, _harness.Clock.Now),
            (ticket.VoidRecord!.VoidFulfillmentTaskId, ticket.VoidRecord.ReasonCode, ticket.VoidRecord.ReasonText, ticket.VoidRecord.ProviderReference, ticket.VoidRecord.ActorId, ticket.VoidRecord.VoidedAt));
        Assert.Equal((task.Id, ElectronicTicketStatus.Voided, 2), (result.VoidFulfillmentTaskId!.Value, result.Tickets.Single().Status, result.Tickets.Single().DocumentVersion));
    }

    [Fact]
    public async Task Another_office_has_no_local_void_authority()
    {
        var (order, tickets) = await IssuedOrderAsync();

        await AssertRefundRequiredAsync(order, 2813, ReservationHarness.IssuingOfficeId + 1, tickets.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Void_at_or_after_the_deadline_requires_a_refund(int minutesAfterDeadline)
    {
        var (order, tickets) = await IssuedOrderAsync();
        _harness.Clock.Now = TehranMidnight.AddMinutes(minutesAfterDeadline);

        await AssertRefundRequiredAsync(order, 2812, ReservationHarness.IssuingOfficeId, tickets.ToArray());
    }

    [Fact]
    public async Task Several_tickets_are_voided_atomically_under_one_local_task()
    {
        var (order, tickets) = await IssuedOrderAsync(TravellerSpec.Adult(1), TravellerSpec.Adult(2));

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        var task = Assert.Single(_harness.Tasks.Committed, item => item.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.All(tickets, ticket => Assert.Equal(task.Id, ticket.VoidRecord!.VoidFulfillmentTaskId));
        Assert.Equal(tickets.Select(ticket => ticket.Id).Order(), TargetIds(task, FulfillmentTargetKind.ElectronicTicket));
        Assert.Equal(tickets.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.Id).Order(), TargetIds(task, FulfillmentTargetKind.TicketCoupon));
    }

    [Fact]
    public async Task One_ineligible_ticket_voids_none_of_the_batch()
    {
        var (order, tickets) = await IssuedOrderAsync(TravellerSpec.Adult(1), TravellerSpec.Adult(2));
        typeof(TicketCoupon).GetProperty(nameof(TicketCoupon.FinancialStatus))!.SetValue(tickets[1].Coupons.Single(), TicketCouponFinancialStatus.Used);

        await AssertRefundRequiredAsync(order, 2815, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        Assert.Equal(TicketCouponFinancialStatus.Open, tickets[0].Coupons.Single().FinancialStatus);
    }

    [Fact]
    public async Task Voiding_already_voided_tickets_adds_no_task_or_version()
    {
        var (order, tickets) = await IssuedOrderAsync();
        var ticket = tickets.Single();
        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, ticket);
        var state = (_harness.Tasks.Committed.Count, ticket.DocumentVersion, _harness.Synchronizer.VoidProjections.Count);

        var repeated = await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, ticket);

        Assert.Equal(state, (_harness.Tasks.Committed.Count, ticket.DocumentVersion, _harness.Synchronizer.VoidProjections.Count));
        Assert.Equal(((long?)null, ElectronicTicketStatus.Voided), (repeated.VoidFulfillmentTaskId, repeated.Tickets.Single().Status));
    }

    [Fact]
    public async Task Mixed_request_voids_only_the_tickets_not_yet_voided()
    {
        var (order, tickets) = await IssuedOrderAsync(TravellerSpec.Adult(1), TravellerSpec.Adult(2));
        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets[0]);
        var firstRecord = tickets[0].VoidRecord;

        var result = await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        var task = _harness.Tasks.Committed.Last(item => item.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.Equal([tickets[1].Id], TargetIds(task, FulfillmentTargetKind.ElectronicTicket));
        Assert.Same(firstRecord, tickets[0].VoidRecord);
        Assert.Equal((2, 2), (tickets[0].DocumentVersion, tickets[1].DocumentVersion));
        Assert.Equal([ElectronicTicketStatus.Voided, ElectronicTicketStatus.Voided], result.Tickets.Select(ticket => ticket.Status));
    }

    [Fact]
    public async Task Stale_document_version_voids_nothing()
    {
        var (order, tickets) = await IssuedOrderAsync();
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.VoidTargetsAsync(
            order,
            ReservationHarness.IssuingOfficeId,
            [new ElectronicTicketVoidTarget(tickets.Single().Id, ExpectedDocumentVersion: 2)]));

        Assert.Equal((2810, 409, savesBefore), (exception.Code, exception.HttpStatus, _harness.UnitOfWork.SaveCount));
        Assert.Equal(ElectronicTicketStatus.Issued, tickets.Single().StatusSummary);
    }

    [Fact]
    public async Task Ticket_of_another_order_is_not_found()
    {
        var (order, _) = await IssuedOrderAsync();
        var (_, foreign) = await IssuedOrderAsync(flightId: 102);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, foreign.Single()));

        Assert.Equal((2809, 404), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public async Task Void_task_is_a_succeeded_local_task_without_provider_interactions()
    {
        var (order, tickets) = await IssuedOrderAsync();

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        var task = Assert.Single(_harness.Tasks.Committed, item => item.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.Equal(
            (OrderFulfillmentStatus.Succeeded, FulfillmentProviderKeys.LocalDocumentAuthority, (long?)null, $"void-ticket:{task.Id}", $"order:{order.Id}:void:{task.Id}", 1, 0),
            (task.Status, task.FulfillmentProviderKey, task.FulfillmentReservationId, task.IdempotencyKey, task.CorrelationReference, task.AttemptCount, task.Interactions.Count));
        Assert.All(task.Targets, target => Assert.Equal(OrderFulfillmentTargetAction.Void, target.Action));
    }

    [Fact]
    public async Task Full_void_with_confirmed_capacity_summarizes_the_order_as_confirmed_and_keeps_services_active()
    {
        var (order, tickets) = await IssuedOrderAsync();
        var providerCalls = (_harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count, _harness.FlightFlow.CancelConfirmedRequests.Count);

        var result = await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        Assert.Equal((OrderStatus.Confirmed, OrderStatus.Confirmed), (order.Status, result.OrderStatus));
        Assert.All(order.Services, service => Assert.True(service.IsActive));
        Assert.Equal(1, order.CommercialVersion);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, _harness.Reservations.Committed.Single().Status);
        Assert.Equal(providerCalls, (_harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count, _harness.FlightFlow.CancelConfirmedRequests.Count));
    }

    [Fact]
    public async Task Partial_void_keeps_the_order_confirmed_while_document_truth_stays_per_ticket()
    {
        var (order, tickets) = await IssuedOrderAsync(TravellerSpec.Adult(1), TravellerSpec.Adult(2));

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets[0]);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal((ElectronicTicketStatus.Voided, ElectronicTicketStatus.Issued), (tickets[0].StatusSummary, tickets[1].StatusSummary));
    }

    [Fact]
    public async Task Issue_void_then_cancel_completes_the_lifecycle()
    {
        var (order, tickets) = await IssuedOrderAsync();
        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        var cancelled = await _harness.CancelAsync(order);

        Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.Equal((OrderStatus.Cancelled, 2), (cancelled.OrderStatus, cancelled.CommercialVersion));
        Assert.Equal(
            [OrderChangeType.Create, OrderChangeType.Cancel],
            order.Changes.OrderBy(change => change.CommercialVersion).Select(change => change.ChangeType));
        Assert.Equal(FulfillmentReservationStatus.Cancelled, _harness.Reservations.Committed.Single().Status);
    }

    [Fact]
    public async Task Void_projects_the_voided_document_and_the_servicing_summary()
    {
        var (order, tickets) = await IssuedOrderAsync();

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, tickets.ToArray());

        var projected = Assert.Single(_harness.Synchronizer.VoidProjections);
        Assert.Equal((ElectronicTicketStatus.Voided, 2, TehranMidnight), (projected.StatusSummary, projected.DocumentVersion, projected.VoidDeadline!.Value));
        Assert.Equal(tickets.Single().VoidRecord!.VoidFulfillmentTaskId, projected.VoidRecord!.VoidFulfillmentTaskId);
        Assert.All(projected.Coupons, coupon => Assert.Equal(TicketCouponFinancialStatus.Void, coupon.FinancialStatus));
        Assert.Equal(OrderStatus.Confirmed, _harness.Synchronizer.ServicingProjections.Last().Status);
    }

    private async Task<(Order Order, IReadOnlyList<ElectronicTicket> Tickets)> IssuedOrderAsync(params TravellerSpec[] travellers)
        => await IssuedOrderAsync(travellers, ReservationHarness.IssuingOfficeId, IssuingOfficeTimeZone, 101);

    private Task<(Order Order, IReadOnlyList<ElectronicTicket> Tickets)> IssuedOrderAsync(long? officeId)
        => IssuedOrderAsync([], officeId, IssuingOfficeTimeZone, 101);

    private Task<(Order Order, IReadOnlyList<ElectronicTicket> Tickets)> IssuedOrderAsync(string? timeZoneId)
        => IssuedOrderAsync([], ReservationHarness.IssuingOfficeId, timeZoneId, 101);

    private Task<(Order Order, IReadOnlyList<ElectronicTicket> Tickets)> IssuedOrderAsync(long flightId)
        => IssuedOrderAsync([], ReservationHarness.IssuingOfficeId, IssuingOfficeTimeZone, flightId);

    private async Task<(Order Order, IReadOnlyList<ElectronicTicket> Tickets)> IssuedOrderAsync(
        TravellerSpec[] travellers,
        long? officeId,
        string? timeZoneId,
        long flightId)
    {
        _harness.OfficeTimeZones.TimeZones[ReservationHarness.IssuingOfficeId] = timeZoneId;

        var order = _harness.SeedOrder(travellers.Length == 0 ? [TravellerSpec.Adult(1)] : travellers, [new BoundSpec("OUT", flightId)]);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);
        var stock = await _harness.DefineStockAsync(prefix: $"09{flightId}", officeId: officeId);
        await _harness.IssueAsync(order, stock.DocumentStockId);

        return (order, _harness.Tickets.Committed.Where(ticket => ticket.CurrentServicingOrderId == order.Id).ToList());
    }

    private async Task AssertRefundRequiredAsync(Order order, int code, long callerOfficeId, params ElectronicTicket[] tickets)
    {
        var savesBefore = _harness.UnitOfWork.SaveCount;
        var versions = tickets.Select(ticket => ticket.DocumentVersion).ToList();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.VoidAsync(order, callerOfficeId, tickets));

        Assert.Equal((code, 409), (exception.Code, exception.HttpStatus));
        Assert.Contains("refund", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(savesBefore, _harness.UnitOfWork.SaveCount);
        Assert.Equal(versions, tickets.Select(ticket => ticket.DocumentVersion));
        Assert.All(tickets, ticket => Assert.Null(ticket.VoidRecord));
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.VoidTicket);
    }

    private static IReadOnlyList<long> TargetIds(FulfillmentTask task, FulfillmentTargetKind kind)
        => task.Targets.Where(target => target.TargetKind == kind).Select(target => target.TargetId).Order().ToList();
}
