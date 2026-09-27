using System.Text.Json;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Flows;

public sealed class LifecycleFlowTests
{
    private readonly ReservationHarness _harness = new();

    public LifecycleFlowTests() => _harness.OfficeTimeZones.TimeZones[ReservationHarness.IssuingOfficeId] = "Asia/Tehran";

    [Fact]
    public async Task Flow_01_create_reserve_confirm_keeps_the_commercial_snapshot_and_one_host_record_locator()
    {
        var order = await CreateAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var accepted = CommercialSnapshotOf(order);

        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        var reservation = ReservationOf(order);
        Assert.Equal(400m, order.CustomerTotal);
        Assert.Equal(accepted, CommercialSnapshotOf(order));
        Assert.Equal(order.RecordLocator, Assert.Single(_harness.RecordLocators.Generated));
        Assert.DoesNotContain(order.RecordLocator, reservation.Units.Select(unit => unit.ProviderUnitRef).Append(reservation.ProviderOperationRef));
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Confirmed, unit.Status));
        Assert.Equal((OrderStatus.Confirmed, FulfillmentReservationStatus.Confirmed), (order.Status, reservation.Status));
    }

    [Fact]
    public async Task Flow_02_create_reserve_confirm_issue_documents_every_active_air_service_exactly_once()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var stock = await _harness.DefineStockAsync();

        var result = await _harness.IssueAsync(order, stock.DocumentStockId);

        var coupons = _harness.Tickets.Committed.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.CurrentOrderServiceId).ToList();
        Assert.Equal(2, _harness.Tickets.Committed.Count);
        Assert.Equal(IdsOf(order.TicketableAirServices()).Order(), coupons.Order());
        Assert.All(_harness.Tickets.Committed, ticket => Assert.Single(ticket.Coupons.Select(coupon => ReservationHarness.AirServices(order).Single(service => service.Id == coupon.CurrentOrderServiceId).TravellerId).Distinct()));
        Assert.Equal(3L, _harness.Stocks.Committed.Single().NextNumber);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.IssueTicket);
        Assert.Equal((OrderStatus.Ticketed, OrderStatus.Ticketed), (order.Status, result.OrderStatus));
    }

    [Fact]
    public async Task Flow_03_reserve_with_an_unknown_outcome_is_recovered_by_replaying_the_exact_persisted_request()
    {
        var order = await CreateAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw Indeterminate("The seat hold timed out."));

        await _harness.ReserveOrderAsync(order);
        var unresolved = ReservationOf(order);
        var unresolvedStatus = unresolved.Status;
        await _harness.ReserveOrderAsync(order);

        var holds = _harness.FlightFlow.HoldRequests;
        Assert.Equal(FulfillmentReservationStatus.Unknown, unresolvedStatus);
        Assert.Equal(2, holds.Count);
        Assert.Equal(JsonSerializer.Serialize(holds[0]), JsonSerializer.Serialize(holds[1]));
        Assert.Same(unresolved, ReservationOf(order));
        Assert.Equal((FulfillmentReservationStatus.Held, OrderStatus.ReservationUnconfirmed), (unresolved.Status, order.Status));
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ReserveInventory);
    }

    [Fact]
    public async Task Flow_04_unknown_confirmation_blocks_issue_until_the_persisted_confirmation_is_replayed()
    {
        var order = await CreateAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        await _harness.ReserveOrderAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw Indeterminate("The seat-hold confirmation timed out."));
        await _harness.ConfirmReservedCapacityAsync(order);
        var stock = await _harness.DefineStockAsync();

        var blocked = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));
        await _harness.ConfirmReservedCapacityAsync(order);
        await _harness.IssueAsync(order, stock.DocumentStockId);

        var confirmations = _harness.FlightFlow.ConfirmRequests;
        Assert.Equal(2777, blocked.Code);
        Assert.Equal(2, confirmations.Count);
        Assert.Equal(confirmations[0], confirmations[1]);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory);
        Assert.Equal((FulfillmentReservationStatus.Confirmed, OrderStatus.Ticketed), (ReservationOf(order).Status, order.Status));
    }

    [Fact]
    public async Task Flow_05_held_order_cancelled_in_full_releases_capacity_before_the_commercial_cancel()
    {
        var order = await CreateAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        await _harness.ReserveOrderAsync(order);
        var reservation = ReservationOf(order);
        var recordLocator = order.RecordLocator;

        await _harness.CancelAsync(order);

        var release = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved);
        var change = order.Changes.Single(item => item.ChangeType == OrderChangeType.Cancel);
        Assert.Single(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, release.Status);
        Assert.True(release.CompletedAt <= change.CommittedAt);
        Assert.Equal((FulfillmentReservationStatus.Released, OrderStatus.Cancelled, recordLocator), (reservation.Status, order.Status, order.RecordLocator));
        Assert.All(order.Services, service => Assert.Equal((OrderServiceCommercialState.Cancelled, (long?)change.Id), (service.CommercialStatus, service.EndedByChangeId)));
        Assert.NotNull(reservation.ProviderOperationRef);
    }

    [Fact]
    public async Task Flow_06_confirmed_order_cancelled_in_full_cancels_the_exact_confirmed_capacity_without_refund_semantics()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var reservation = ReservationOf(order);
        var pricing = (order.CustomerTotal, order.PricingLines.Count);

        await _harness.CancelAsync(order);

        var cancellation = Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.Equal(reservation.Units.Select(unit => unit.ProviderUnitRef!).Order(), cancellation.SeatHoldReferences.Order());
        Assert.Equal((FulfillmentReservationStatus.Cancelled, OrderStatus.Cancelled), (reservation.Status, order.Status));
        Assert.Equal(pricing, (order.CustomerTotal, order.PricingLines.Count));
        Assert.Empty(_harness.Tickets.Committed);
    }

    [Fact]
    public async Task Flow_07_lost_confirmed_cancellation_is_recovered_only_within_its_persisted_target_scope()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var inbound = ReservationHarness.AirService(order, 1, 201);
        _harness.FlightFlow.CancelConfirmedResponses.Enqueue(_ => throw Indeterminate("The confirmed-seat cancellation timed out."));
        await _harness.CancelAsync(order, outbound.Id);

        var blocked = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, inbound.Id));
        var callsWhenBlocked = _harness.FlightFlow.CancelConfirmedRequests.Count;
        await _harness.CancelAsync(order, outbound.Id);

        var requests = _harness.FlightFlow.CancelConfirmedRequests;
        Assert.Equal((2778, 1), (blocked.Code, callsWhenBlocked));
        Assert.Equal(requests[0].SeatHoldReferences, requests[1].SeatHoldReferences);
        Assert.Equal((OrderServiceCommercialState.Cancelled, OrderServiceCommercialState.Active), (outbound.CommercialStatus, inbound.CommercialStatus));
        Assert.Equal(2, order.CommercialVersion);
    }

    [Fact]
    public async Task Flow_08_void_keeps_the_service_and_capacity_until_a_later_cancel_ends_them()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);
        var ticket = _harness.Tickets.Committed.Single();

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, ticket);

        var afterVoid = (ticket.StatusSummary, ReservationOf(order).Status, order.Status, order.Services.All(service => service.IsActive));
        await _harness.CancelAsync(order);

        Assert.Equal((ElectronicTicketStatus.Voided, FulfillmentReservationStatus.Confirmed, OrderStatus.Confirmed, true), afterVoid);
        Assert.Equal((FulfillmentReservationStatus.Cancelled, OrderStatus.Cancelled), (ReservationOf(order).Status, order.Status));
        Assert.Equal((ElectronicTicketStatus.Voided, 2), (ticket.StatusSummary, ticket.DocumentVersion));
        Assert.NotNull(ticket.VoidRecord);
        Assert.Equal(
            [OrderFulfillmentTaskType.ReserveInventory, OrderFulfillmentTaskType.ConfirmInventory, OrderFulfillmentTaskType.IssueTicket, OrderFulfillmentTaskType.VoidTicket, OrderFulfillmentTaskType.CancelConfirmed],
            _harness.Tasks.Committed.Where(task => task.OrderId == order.Id).Select(task => task.TaskType));
        Assert.All(_harness.Tasks.Committed, task => Assert.Equal(OrderFulfillmentStatus.Succeeded, task.Status));
        Assert.Equal([OrderChangeType.Create, OrderChangeType.Cancel], order.Changes.Select(change => change.ChangeType));
    }

    [Fact]
    public async Task Flow_09_late_expiry_after_issue_neither_expires_the_order_nor_touches_its_capacity()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);

        _harness.Clock.Now = lastTicketingDate.AddHours(3);
        var due = await _harness.DueOrderIdsAsync();
        await _harness.EnforceDeadlinesAsync(order);

        Assert.Empty(due);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal((OrderStatus.Ticketed, FulfillmentReservationStatus.Confirmed), (order.Status, ReservationOf(order).Status));
        Assert.Equal(ElectronicTicketStatus.Issued, _harness.Tickets.Committed.Single().StatusSummary);
    }

    [Fact]
    public async Task Flow_10_independent_one_way_remainder_is_issued_after_its_counterpart_is_cancelled()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal([outbound.Id], _harness.Tickets.Committed.Single().Coupons.Select(coupon => coupon.CurrentOrderServiceId));
        Assert.Equal((OrderStatus.Ticketed, FulfillmentReservationStatus.Mixed), (order.Status, ReservationOf(order).Status));
        Assert.Equal(ReservationMemberStatus.Confirmed, ReservationOf(order).Units.Single(unit => unit.OrderServiceIds.Contains(outbound.Id)).Status);
    }

    [Fact]
    public async Task Flow_11_round_trip_remainder_requires_repricing_before_any_pricing_call_or_document_effect()
    {
        var order = await ConfirmedAsync(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)],
            [PricingUnitSpec.RoundTripFare(["OUT", "RET"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);

        await AssertRepricingRequiredAsync(order);
    }

    [Fact]
    public async Task Flow_12_through_fare_remainder_requires_repricing()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 102).Id);

        await AssertRepricingRequiredAsync(order);
    }

    [Fact]
    public async Task Flow_13_commercial_change_makes_unexpired_validation_stale_so_issue_revalidates()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);
        var stock = await _harness.DefineStockAsync();
        var before = ReservationOf(order).ValidationEvidence!;
        var validations = _harness.AirFareValidator.Calls.Count;

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var after = ReservationOf(order).ValidationEvidence!;
        Assert.True(before.ValidUntil > _harness.Clock.Now);
        Assert.Equal((1, 2, 2), (before.CommercialVersion, order.CommercialVersion, after.CommercialVersion));
        Assert.Equal(validations + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal([outbound.Id], after.ValidatedOrderServiceIds);
    }

    [Fact]
    public async Task Flow_14_terminal_reservation_stays_history_and_a_new_reserve_uses_a_new_identity()
    {
        var order = await CreateAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: _harness.Clock.Now.AddDays(2));
        var released = await _harness.ReserveOrderAsync(order);
        await _harness.ReleaseAsync(order, released.Reservations.Single().ReservationId);
        var recordLocator = order.RecordLocator;

        var renewed = await _harness.ReserveOrderAsync(order);

        var reservations = _harness.Reservations.Committed.Where(reservation => reservation.OrderId == order.Id).ToList();
        var holds = _harness.FlightFlow.HoldRequests;
        Assert.Equal(2, reservations.Count);
        Assert.NotEqual(released.Reservations.Single().ReservationId, renewed.Reservations.Single().ReservationId);
        Assert.Equal((FulfillmentReservationStatus.Released, FulfillmentReservationStatus.Held), (reservations[0].Status, reservations[1].Status));
        Assert.NotEqual(holds[0].IdempotencyKey, holds[1].IdempotencyKey);
        Assert.Equal(2, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(recordLocator, order.RecordLocator);
    }

    [Fact]
    public async Task Flow_15_repeated_issue_allocates_no_second_document()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        var first = await _harness.IssueAsync(order, stock.DocumentStockId);

        var repeated = await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(first.IssueFulfillmentTaskId, repeated.IssueFulfillmentTaskId);
        Assert.Equal(first.Tickets.Select(ticket => ticket.DocumentNumber), repeated.Tickets.Select(ticket => ticket.DocumentNumber));
        Assert.Single(_harness.Tickets.Committed);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.IssueTicket);
        Assert.Equal(2L, _harness.Stocks.Committed.Single().NextNumber);
    }

    [Fact]
    public async Task Flow_16_repeated_void_and_cancel_create_no_second_history_or_provider_effect()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);
        var ticket = _harness.Tickets.Committed.Single();

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, ticket);
        var voided = (ticket.VoidRecord, ticket.DocumentVersion);
        await _harness.VoidTargetsAsync(order, ReservationHarness.IssuingOfficeId, [new(ticket.Id, ticket.DocumentVersion)]);
        await _harness.CancelAsync(order);
        var cancelled = (order.CommercialVersion, order.Changes.Count, _harness.FlightFlow.CancelConfirmedRequests.Count);
        await _harness.CancelAsync(order);

        Assert.Equal(voided, (ticket.VoidRecord, ticket.DocumentVersion));
        Assert.Equal(cancelled, (order.CommercialVersion, order.Changes.Count, _harness.FlightFlow.CancelConfirmedRequests.Count));
        Assert.Equal((2, 2, 1), cancelled);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.CancelConfirmed);
    }

    private Task<Order> CreateAsync(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<PricingUnitSpec>? pricingUnits = null,
        DateTimeOffset? lastTicketingDate = null)
        => _harness.CreateFromOfferAsync(travellers, bounds, lastTicketingDate: lastTicketingDate, pricingUnits: pricingUnits);

    private async Task<Order> ConfirmedAsync(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<PricingUnitSpec>? pricingUnits = null,
        DateTimeOffset? lastTicketingDate = null)
    {
        var order = await CreateAsync(travellers, bounds, pricingUnits, lastTicketingDate);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        return order;
    }

    private FulfillmentReservation ReservationOf(Order order)
        => _harness.Reservations.Committed.Single(reservation => reservation.OrderId == order.Id);

    private async Task AssertRepricingRequiredAsync(Order order)
    {
        var stock = await _harness.DefineStockAsync();
        var validations = _harness.AirFareValidator.Calls.Count;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));

        Assert.Equal((2820, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal(validations, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(1L, _harness.Stocks.Committed.Single().NextNumber);
        Assert.Empty(_harness.Tickets.Committed);
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.IssueTicket);
    }

    private static (decimal CustomerTotal, int CommercialVersion, int PricingLines, int PricingUnits, DateTimeOffset? LastTicketingDate) CommercialSnapshotOf(Order order)
        => (order.CustomerTotal, order.CommercialVersion, order.PricingLines.Count, order.FarePricingUnits.Count, order.LastTicketingDate);

    private static IEnumerable<long> IdsOf(IEnumerable<OrderService> services) => services.Select(service => service.Id);

    private static ProviderRequestException Indeterminate(string message)
        => new(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, message);
}
