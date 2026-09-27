using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class IssueAfterPartialCancellationTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Independent_one_way_pricing_unit_is_issued_from_a_mixed_reservation_after_the_other_is_cancelled()
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();

        Assert.Equal(FulfillmentReservationStatus.Mixed, Reservation.Status);

        var result = await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal([outbound.Id], Assert.Single(_harness.Tickets.Committed).Coupons.Select(coupon => coupon.CurrentOrderServiceId));
        Assert.Equal((OrderStatus.Ticketed, OrderStatus.Ticketed), (order.Status, result.OrderStatus));
        Assert.Equal(FulfillmentReservationStatus.Mixed, Reservation.Status);
        Assert.Equal((ReservationMemberStatus.Confirmed, ReservationMemberStatus.Cancelled), (UnitOf(outbound).Status, UnitOf(inbound).Status));
    }

    [Fact]
    public async Task Intact_fare_component_of_a_one_way_pricing_unit_is_issued_after_the_other_component_is_cancelled()
    {
        var order = await ConfirmedAsync(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            [PricingUnitSpec.ThroughOneWay("OUT", new FareSpec(8001, 101), new FareSpec(8002, 102))]);
        var first = ReservationHarness.AirService(order, 1, 101);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 102).Id);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal([first.Id], Assert.Single(_harness.Tickets.Committed).Coupons.Select(coupon => coupon.CurrentOrderServiceId));
        Assert.Equal((OrderStatus.Ticketed, FulfillmentReservationStatus.Mixed), (order.Status, Reservation.Status));
    }

    [Fact]
    public async Task Cancellation_makes_unexpired_evidence_stale_so_issue_revalidates_the_remaining_scope()
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();
        var evidence = Reservation.ValidationEvidence!;
        var calls = _harness.AirFareValidator.Calls.Count;

        Assert.True(evidence.ValidUntil > _harness.Clock.Now);
        Assert.Equal((1, 2), (evidence.CommercialVersion, order.CommercialVersion));

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(calls + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal([outbound.Id], _harness.AirFareValidator.Calls[^1]);
    }

    [Fact]
    public async Task Issue_refresh_persists_the_current_version_the_exact_scope_and_the_returned_validity()
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();
        var validUntil = _harness.Clock.Now.AddHours(3);
        _harness.AirFareValidator.Behavior = (_, _) => validUntil;

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var evidence = Reservation.ValidationEvidence!;
        Assert.Equal((2, validUntil, _harness.Clock.Now), (evidence.CommercialVersion, evidence.ValidUntil, evidence.ValidatedAt));
        Assert.Equal([outbound.Id], evidence.ValidatedOrderServiceIds);
        Assert.Equal(validUntil, Reservation.ReservationValidationTimeLimit);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Current_evidence_that_does_not_cover_the_issue_scope_is_refreshed()
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        var stock = await _harness.DefineStockAsync();
        Reservation.RecordValidation(new ReservationValidationEvidence(order.CommercialVersion, _harness.Clock.Now.AddDays(1), _harness.Clock.Now, [outbound.Id]));
        var calls = _harness.AirFareValidator.Calls.Count;

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(calls + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(new[] { outbound.Id, inbound.Id }.Order(), _harness.AirFareValidator.Calls[^1].Order());
        Assert.Equal(new[] { outbound.Id, inbound.Id }.Order(), Reservation.ValidationEvidence!.ValidatedOrderServiceIds.Order());
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Round_trip_fracture_blocks_issue_before_any_pricing_call_or_document_effect()
    {
        var order = await ConfirmedAsync(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)],
            [PricingUnitSpec.RoundTripFare(["OUT", "RET"], new FareSpec(9001, 101), new FareSpec(9002, 201))]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);

        await AssertFractureBlocksIssueAsync(order);
    }

    [Fact]
    public async Task Through_fare_fracture_blocks_issue_of_the_remaining_segment()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 102).Id);

        await AssertFractureBlocksIssueAsync(order);
    }

    [Theory]
    [InlineData(FarePricingUnitType.OpenJaw)]
    [InlineData(FarePricingUnitType.CircleTrip)]
    public async Task Open_jaw_or_circle_trip_fracture_blocks_issue(FarePricingUnitType semanticType)
    {
        var order = await ConfirmedAsync(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)],
            [new PricingUnitSpec($"{semanticType}Fare", semanticType, ["OUT", "RET"], new FareSpec(9101, 101), new FareSpec(9102, 201))]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);

        await AssertFractureBlocksIssueAsync(order);
    }

    [Theory]
    [InlineData(FarePricingUnitType.Unspecified)]
    [InlineData(FarePricingUnitType.Other)]
    public async Task Unmapped_pricing_unit_is_conservatively_blocked_after_a_partial_cancellation(FarePricingUnitType semanticType)
    {
        var order = await ConfirmedAsync(
            [TravellerSpec.Adult(1)],
            [new BoundSpec("OUT", 101, 102)],
            [new PricingUnitSpec("SectorSum", semanticType, ["OUT"], new FareSpec(8001, 101), new FareSpec(8002, 102))]);
        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 102).Id);

        await AssertFractureBlocksIssueAsync(order);
    }

    [Theory]
    [InlineData(ReservationMemberStatus.Unknown)]
    [InlineData(ReservationMemberStatus.Cancelled)]
    [InlineData(ReservationMemberStatus.Released)]
    [InlineData(ReservationMemberStatus.Expired)]
    public async Task Target_unit_that_is_not_confirmed_blocks_issue_from_a_mixed_reservation(ReservationMemberStatus status)
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();
        typeof(ReservationUnit).GetProperty(nameof(ReservationUnit.Status))!.SetValue(UnitOf(outbound), status);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));

        Assert.Equal((2777, 409), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.Tickets.Committed);
    }

    [Fact]
    public async Task Issue_refresh_is_validation_only_and_never_replans_or_mutates_the_reservation()
    {
        var (order, outbound, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();
        var providerCalls = ProviderCalls();
        var reservation = (Reservation.Status, Reservation.ProviderOperationRef, Reservation.ExpiresAt);
        var units = Reservation.Units.Select(unit => (unit.Id, unit.Status, unit.ProviderUnitRef, string.Join(",", unit.OrderServiceIds))).ToList();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(providerCalls, ProviderCalls());
        Assert.Equal(reservation, (Reservation.Status, Reservation.ProviderOperationRef, Reservation.ExpiresAt));
        Assert.Equal(units, Reservation.Units.Select(unit => (unit.Id, unit.Status, unit.ProviderUnitRef, string.Join(",", unit.OrderServiceIds))));
        Assert.Equal(2, Reservation.Units.Count);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Legacy_timestamp_without_canonical_evidence_is_revalidated_before_issue()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var stock = await _harness.DefineStockAsync();
        typeof(FulfillmentReservation).GetProperty(nameof(FulfillmentReservation.ValidationEvidence))!.SetValue(Reservation, null);
        var calls = _harness.AirFareValidator.Calls.Count;

        Assert.True(Reservation.ReservationValidationTimeLimit > _harness.Clock.Now);

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(calls + 1, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(1, Reservation.ValidationEvidence!.CommercialVersion);
        Assert.Equal([outbound.Id], Reservation.ValidationEvidence.ValidatedOrderServiceIds);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Lost_cancellation_of_an_outstanding_service_blocks_issue_through_its_unit()
    {
        var (order, _, inbound) = await ConfirmedOutboundAndInboundAsync();
        _harness.FlightFlow.CancelConfirmedResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The confirmed-seat cancellation timed out."));
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));

        Assert.Equal(2777, exception.Code);
        Assert.Equal(ReservationMemberStatus.Unknown, UnitOf(inbound).Status);
        Assert.Empty(_harness.Tickets.Committed);
    }

    [Fact]
    public async Task Unresolved_effect_on_another_unit_of_the_same_reservation_conservatively_blocks_issue()
    {
        var (order, _, inbound) = await ConfirmedOutboundAndInboundAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var stock = await _harness.DefineStockAsync();
        var unresolved = FulfillmentTask.Create(
            _harness.Ids.NewId(),
            order.Id,
            Reservation.Id,
            OrderFulfillmentTaskType.CancelConfirmed,
            FulfillmentProviderKeys.FlightFlow,
            "cancel-confirmed:unresolved",
            "order:unresolved",
            FulfillmentTargetKind.ReservationUnit,
            [UnitOf(inbound).Id],
            OrderFulfillmentTargetAction.Cancel,
            _harness.Ids,
            _harness.Clock.Now);
        unresolved.StartAttempt(_harness.Ids, _harness.Clock.Now);
        await _harness.Tasks.AddAsync(unresolved);
        await _harness.UnitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));

        Assert.Equal(2778, exception.Code);
        Assert.Empty(_harness.Tickets.Committed);
    }

    private FulfillmentReservation Reservation => _harness.Reservations.Committed.Single();

    private ReservationUnit UnitOf(OrderService service)
        => Reservation.Units.Single(unit => unit.OrderServiceIds.Contains(service.Id));

    private (int Holds, int Confirms, int Releases, int Cancels) ProviderCalls()
        => (_harness.FlightFlow.HoldRequests.Count, _harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count, _harness.FlightFlow.CancelConfirmedRequests.Count);

    private async Task<(Order Order, OrderService Outbound, OrderService Inbound)> ConfirmedOutboundAndInboundAsync()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);

        return (order, ReservationHarness.AirService(order, 1, 101), ReservationHarness.AirService(order, 1, 201));
    }

    private async Task<Order> ConfirmedAsync(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<PricingUnitSpec>? pricingUnits = null)
    {
        var order = _harness.SeedOrder(travellers, bounds, pricingUnits: pricingUnits);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        return order;
    }

    private async Task AssertFractureBlocksIssueAsync(Order order)
    {
        var stock = await _harness.DefineStockAsync();
        var validations = _harness.AirFareValidator.Calls.Count;
        var saves = _harness.UnitOfWork.SaveCount;
        var locks = _harness.Lock.Acquired.Count;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.IssueAsync(order, stock.DocumentStockId));

        Assert.Equal((2820, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal((validations, saves), (_harness.AirFareValidator.Calls.Count, _harness.UnitOfWork.SaveCount));
        Assert.Equal(1L, _harness.Stocks.Committed.Single().NextNumber);
        Assert.Equal([$"reservation:{order.Id}"], _harness.Lock.Acquired.Skip(locks));
        Assert.Empty(_harness.Tickets.Committed);
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.IssueTicket);
        Assert.NotEqual(OrderStatus.Ticketed, order.Status);
    }
}
