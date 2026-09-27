using System.Net;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Domain._Shared.Resources;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class IssueEligibilityTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Held_capacity_blocks_issue()
    {
        var order = SeedOrder();
        await _harness.ReserveOrderAsync(order);

        await AssertBlockedAsync(order, 2777);
    }

    [Fact]
    public async Task Unknown_confirmation_blocks_issue()
    {
        var order = SeedOrder();
        await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The seat-hold confirmation timed out."));
        await _harness.ConfirmReservedCapacityAsync(order);

        await AssertBlockedAsync(order, 2777);
    }

    [Fact]
    public async Task Mixed_reservation_blocks_issue()
    {
        var order = SeedOrder();
        await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ConfirmWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1179)));
        await _harness.ConfirmReservedCapacityAsync(order);

        await AssertBlockedAsync(order, 2777);
    }

    [Theory]
    [InlineData(1176)]
    [InlineData(1178)]
    public async Task Expired_or_cancelled_hold_blocks_issue(int refusalCode)
    {
        var order = SeedOrder();
        await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ConfirmWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(refusalCode)));
        await _harness.ConfirmReservedCapacityAsync(order);

        await AssertBlockedAsync(order, 2777);
    }

    [Fact]
    public async Task Released_hold_blocks_issue()
    {
        var order = SeedOrder();
        var reserved = await _harness.ReserveOrderAsync(order);
        await _harness.ReleaseAsync(order, reserved.Reservations.Single().ReservationId);

        await AssertBlockedAsync(order, 2777);
    }

    [Fact]
    public async Task Unreserved_capacity_blocks_issue()
        => await AssertBlockedAsync(SeedOrder(), 2800);

    [Fact]
    public async Task Service_without_reservation_requirement_is_ticketed_without_a_fabricated_reservation()
    {
        var provider = new StubReservationProvider("ProviderN", ReservationMode.None, ReservationMemberStatus.Confirmed);
        var harness = new ReservationHarness(provider);
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, provider.ProviderKey);
        var stock = await harness.DefineStockAsync();

        await harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Empty(harness.Reservations.Committed);
        Assert.Empty(provider.ReserveCalls);
        Assert.Single(harness.Tickets.Committed);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Stale_validation_is_renewed_through_air_price_before_any_number_is_allocated()
    {
        var validUntil = _harness.Clock.Now.AddHours(1);
        var renewedUntil = validUntil.AddHours(2);
        var order = await ConfirmedOrderAsync(validUntil);
        var stock = await _harness.DefineStockAsync();
        var calls = _harness.AirFareValidator.Calls.Count;

        _harness.Clock.Now = validUntil;
        _harness.AirFareValidator.Behavior = (_, _) =>
        {
            Assert.Equal(1L, _harness.Stocks.Committed.Single().NextNumber);
            return renewedUntil;
        };
        await _harness.IssueAsync(order, stock.DocumentStockId);

        var reservation = _harness.Reservations.Committed.Single();
        Assert.Equal(calls + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal((renewedUntil, FulfillmentReservationStatus.Confirmed), (reservation.ReservationValidationTimeLimit!.Value, reservation.Status));
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Rejected_air_price_revalidation_has_no_issue_effect()
    {
        var validUntil = _harness.Clock.Now.AddHours(1);
        var order = await ConfirmedOrderAsync(validUntil);
        var stock = await _harness.DefineStockAsync();

        _harness.Clock.Now = validUntil;
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FareReservationIsNotPermitted("The fare is no longer valid.");

        await AssertBlockedAsync(order, 2608, stock.DocumentStockId);
    }

    [Fact]
    public async Task Unsupported_fare_topology_has_no_issue_effect()
    {
        var validUntil = _harness.Clock.Now.AddHours(1);
        var order = await ConfirmedOrderAsync(validUntil);
        var stock = await _harness.DefineStockAsync();

        _harness.Clock.Now = validUntil;
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FarePricingUnitValidationIsUnsupported(1, "SectorSum");

        await AssertBlockedAsync(order, 2768, stock.DocumentStockId);
    }

    [Fact]
    public async Task Passed_last_ticketing_date_has_no_issue_effect()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = await ConfirmedOrderAsync(lastTicketingDate: lastTicketingDate);
        _harness.Clock.Now = lastTicketingDate;

        await AssertBlockedAsync(order, 2775);
    }

    [Fact]
    public async Task Absent_last_ticketing_date_invents_no_deadline()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync();
        _harness.Clock.Now = _harness.Clock.Now.AddDays(300);

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Null(order.LastTicketingDate);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Missing_stock_has_no_issue_effect()
        => await AssertBlockedAsync(await ConfirmedOrderAsync(), 2785, ticketDocumentStockId: 404);

    [Fact]
    public async Task Stock_of_another_document_kind_has_no_issue_effect()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync(prefix: "0992", documentKind: AccountableDocumentKind.ElectronicMiscDocument);

        await AssertBlockedAsync(order, 2786, stock.DocumentStockId);
    }

    [Fact]
    public async Task Exhausted_stock_has_no_issue_effect()
    {
        var stock = await _harness.DefineStockAsync(rangeFrom: 1, rangeTo: 1);
        await _harness.IssueAsync(await ConfirmedOrderAsync(flightId: 101), stock.DocumentStockId);
        var order = await ConfirmedOrderAsync(flightId: 102);

        await AssertBlockedAsync(order, 2788, stock.DocumentStockId);
    }

    [Fact]
    public async Task Stock_short_of_the_whole_plan_commits_no_ticket()
    {
        var stock = await _harness.DefineStockAsync(rangeFrom: 1, rangeTo: 1);
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        await AssertBlockedAsync(order, 2789, stock.DocumentStockId);
    }

    [Fact]
    public async Task Missing_fare_component_blocks_before_allocation()
    {
        var order = await ConfirmedOrderAsync();
        var service = ReservationHarness.AirService(order, 1, 101);
        CoveredServiceIdsOf(ComponentOf(order, service)).Remove(service.Id);

        await AssertBlockedAsync(order, 2779);
    }

    [Fact]
    public async Task Ambiguous_fare_component_blocks_before_allocation()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        CoveredServiceIdsOf(ComponentOf(order, ReservationHarness.AirService(order, 1, 201))).Add(outbound.Id);

        await AssertBlockedAsync(order, 2780);
    }

    [Theory]
    [InlineData(OrderFulfillmentTaskType.ConfirmInventory)]
    [InlineData(OrderFulfillmentTaskType.ReleaseReserved)]
    [InlineData(OrderFulfillmentTaskType.IssueTicket)]
    public async Task Unresolved_overlapping_effect_blocks_issue(OrderFulfillmentTaskType taskType)
    {
        var order = await ConfirmedOrderAsync();
        var reservation = _harness.Reservations.Committed.Single();
        await _harness.Tasks.AddAsync(FulfillmentTask.Create(
            _harness.Ids.NewId(),
            order.Id,
            taskType == OrderFulfillmentTaskType.IssueTicket ? null : reservation.Id,
            taskType,
            FulfillmentProviderKeys.FlightFlow,
            "unresolved:1",
            "order:unresolved",
            FulfillmentTargetKind.ReservationUnit,
            reservation.Units.Select(unit => unit.Id).ToList(),
            OrderFulfillmentTargetAction.Sync,
            _harness.Ids,
            _harness.Clock.Now));
        await _harness.UnitOfWork.SaveChangesAsync();

        await AssertBlockedAsync(order, 2778);
    }

    [Fact]
    public async Task Completed_effects_do_not_block_issue()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.All(_harness.Tasks.Committed, task => Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status));
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Late_deadline_enforcement_never_expires_a_ticketed_order()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = await ConfirmedOrderAsync(lastTicketingDate: lastTicketingDate);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);

        _harness.Clock.Now = lastTicketingDate.AddHours(1);
        var due = await _harness.DueOrderIdsAsync();
        await _harness.EnforceDeadlinesAsync(order);

        Assert.Empty(due);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
    }

    [Fact]
    public async Task Issue_holds_the_order_lock_before_the_stock_lock()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync();
        var acquiredBefore = _harness.Lock.Acquired.Count;

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal([$"reservation:{order.Id}", $"document-stock:{stock.DocumentStockId}"], _harness.Lock.Acquired.Skip(acquiredBefore));
    }

    [Fact]
    public async Task Issue_while_another_order_operation_runs_has_no_effect()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync();
        _harness.Lock.Hold($"reservation:{order.Id}");

        await AssertBlockedAsync(order, 2512, stock.DocumentStockId);
    }

    [Fact]
    public async Task Two_orders_on_one_stock_take_distinct_consecutive_numbers()
    {
        var stock = await _harness.DefineStockAsync();
        var first = await ConfirmedOrderAsync(flightId: 101);
        var second = await ConfirmedOrderAsync(flightId: 102);

        await _harness.IssueAsync(first, stock.DocumentStockId);
        await _harness.IssueAsync(second, stock.DocumentStockId);

        Assert.Equal(["09910000000001", "09910000000002"], _harness.Tickets.Committed.Select(ticket => ticket.DocumentNumber));
        Assert.Equal(3L, _harness.Stocks.Committed.Single().NextNumber);
    }

    [Fact]
    public async Task Stock_consumed_between_planning_and_the_stock_lock_commits_no_ticket()
    {
        var order = await ConfirmedOrderAsync();
        var stock = await _harness.DefineStockAsync(rangeFrom: 1, rangeTo: 1);
        _harness.Stocks.OnReload = reloaded => reloaded.Allocate(999, "ETKT:elsewhere:1", _harness.Ids, _harness.Clock.Now);

        await AssertBlockedAsync(order, 2788, stock.DocumentStockId);
    }

    [Fact]
    public async Task Stock_definition_is_readable_and_overlapping_ranges_are_rejected()
    {
        var defined = await _harness.DefineStockAsync(rangeFrom: 1, rangeTo: 100, officeId: 34);
        var otherPrefix = await _harness.DefineStockAsync(rangeFrom: 50, rangeTo: 150, prefix: "0993");

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.DefineStockAsync(rangeFrom: 50, rangeTo: 150));

        Assert.Equal((DocumentStockStatus.Active, 1L, 34L), (defined.Status, defined.NextNumber, defined.OfficeId!.Value));
        Assert.Equal(defined.DocumentStockId, _harness.Synchronizer.StockProjections.First().DocumentStockId);
        Assert.Equal((2792, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal(2, _harness.Stocks.Committed.Count);
        Assert.NotEqual(defined.DocumentStockId, otherPrefix.DocumentStockId);
    }

    [Fact]
    public async Task Unsupported_check_digit_profile_cannot_define_a_stock()
    {
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.DefineStockAsync(checkDigitProfile: "Mod7"));

        Assert.Equal((2790, 422), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.Stocks.Committed);
    }

    private Order SeedOrder(long flightId = 101, DateTimeOffset? lastTicketingDate = null)
        => _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", flightId)], lastTicketingDate: lastTicketingDate);

    private async Task<Order> ConfirmedOrderAsync(DateTimeOffset? validUntil = null, DateTimeOffset? lastTicketingDate = null, long flightId = 101)
    {
        if (validUntil is { } timeLimit)
            _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        var order = SeedOrder(flightId, lastTicketingDate);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);
        return order;
    }

    private async Task AssertBlockedAsync(Order order, int code, long? ticketDocumentStockId = null)
    {
        var stockId = ticketDocumentStockId ?? (await _harness.DefineStockAsync(prefix: $"09{order.Id % 100:00}")).DocumentStockId;
        var statusBefore = order.Status;
        var ticketsBefore = _harness.Tickets.Committed.Count;
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stockId));

        Assert.Equal(code, exception.Code);
        Assert.Equal(statusBefore, order.Status);
        Assert.Equal(ticketsBefore, _harness.Tickets.Committed.Count);
        Assert.Equal(savesBefore, _harness.UnitOfWork.SaveCount);
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.IssueTicket && task.Status == OrderFulfillmentStatus.Succeeded && task.OrderId == order.Id);
    }

    private static OrderFareComponentAccess ComponentOf(Order order, OrderAirTransportService service)
        => new(order.FarePricingUnits.SelectMany(unit => unit.FareComponents).Single(component => component.CoveredOrderServiceIds.Contains(service.Id)));

    private static List<long> CoveredServiceIdsOf(OrderFareComponentAccess component)
        => (List<long>)typeof(OrderFareComponent)
            .GetField("_coveredOrderServiceIds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(component.Component)!;

    private sealed record OrderFareComponentAccess(OrderFareComponent Component);
}
