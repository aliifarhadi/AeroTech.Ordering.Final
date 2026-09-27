using System.Net;
using System.Text.Json;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class CommercialCancellationTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Held_order_is_released_then_cancelled()
    {
        var order = SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);
        var reservation = await ReserveAsync(order);

        var result = await _harness.CancelAsync(order);

        var release = Assert.Single(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(reservation.ProviderOperationRef, release.HoldId);
        Assert.Equal(FulfillmentReservationStatus.Released, reservation.Status);
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
        AssertCommitted(order, result);
    }

    [Fact]
    public async Task Confirmed_order_cancels_its_confirmed_capacity_then_is_cancelled()
    {
        var order = SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);
        var reservation = await ConfirmAsync(order);

        var result = await _harness.CancelAsync(order);

        var cancellation = Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.Equal(reservation.ProviderOperationRef, cancellation.HoldBatchId);
        Assert.Equal(reservation.Units.Select(unit => unit.ProviderUnitRef!).Order(), cancellation.SeatHoldReferences.Order());
        Assert.Equal(FulfillmentReservationStatus.Cancelled, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Cancelled, unit.Status));
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        AssertCommitted(order, result);
    }

    [Fact]
    public async Task Cancel_confirmed_task_carries_its_derived_identity_and_the_persisted_exact_request()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ConfirmAsync(order);

        await _harness.CancelAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed);
        var interaction = Assert.Single(task.Interactions);
        var sent = Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.Equal(
            ($"cancel-confirmed:{task.Id}", $"order:{order.Id}:reservation:{reservation.Id}:cancel:{task.Id}", OrderFulfillmentStatus.Succeeded),
            (task.IdempotencyKey, task.CorrelationReference, task.Status));
        Assert.Equal(
            reservation.Units.Select(unit => (FulfillmentTargetKind.ReservationUnit, unit.Id, OrderFulfillmentTargetAction.Cancel)),
            task.Targets.Select(target => (target.TargetKind, target.TargetId, target.Action)));
        Assert.Equal((ProviderInteractionType.CancelConfirmed, (string?)null), (interaction.InteractionType, interaction.IdempotencyKey));
        var persisted = JsonSerializer.Deserialize<CancelConfirmedSeatsRequest>(interaction.RequestPayload, JsonOptions)!;
        Assert.Equal((sent.HoldBatchId, sent.ReasonCode), (persisted.HoldBatchId, persisted.ReasonCode));
        Assert.Equal(sent.SeatHoldReferences, persisted.SeatHoldReferences);
        Assert.Equal(FlightSeatHoldCancellationReason.PaxRequest, sent.ReasonCode);
    }

    [Fact]
    public async Task Targeted_confirmed_segment_cancels_only_its_unit_and_keeps_the_rest_active()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)], new BoundSpec("OUT", 101), new BoundSpec("RET", 201));
        var reservation = await ConfirmAsync(order);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var inbound = ReservationHarness.AirService(order, 1, 201);

        var result = await _harness.CancelAsync(order, inbound.Id);

        var inboundUnit = reservation.Units.Single(unit => unit.OrderServiceIds.Contains(inbound.Id));
        var outboundUnit = reservation.Units.Single(unit => unit.OrderServiceIds.Contains(outbound.Id));
        Assert.Equal([inboundUnit.ProviderUnitRef!], Assert.Single(_harness.FlightFlow.CancelConfirmedRequests).SeatHoldReferences);
        Assert.Equal(
            (ReservationMemberStatus.Cancelled, ReservationMemberStatus.Confirmed, FulfillmentReservationStatus.Mixed),
            (inboundUnit.Status, outboundUnit.Status, reservation.Status));
        Assert.Equal((OrderServiceCommercialState.Cancelled, OrderServiceCommercialState.Active), (inbound.CommercialStatus, outbound.CommercialStatus));
        Assert.Equal([inbound.Id], result.CancelledServiceIds);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Part_of_a_shared_held_operation_cannot_be_cancelled_and_nothing_changes()
    {
        var order = SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);
        var reservation = await ReserveAsync(order);
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 101).Id));

        Assert.Equal((2805, 422), (exception.Code, exception.HttpStatus));
        Assert.Equal((savesBefore, FulfillmentReservationStatus.Held, 1), (_harness.UnitOfWork.SaveCount, reservation.Status, order.CommercialVersion));
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.All(order.Services, service => Assert.True(service.IsActive));
    }

    [Fact]
    public async Task Released_capacity_is_cancelled_commercially_without_a_new_provider_call()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ReserveAsync(order);
        await _harness.ReleaseAsync(order, reservation.Id);
        var providerCalls = ProviderCalls();

        var result = await _harness.CancelAsync(order);

        Assert.Equal(providerCalls, ProviderCalls());
        AssertCommitted(order, result);
    }

    [Fact]
    public async Task Expired_capacity_is_cancelled_commercially_without_a_new_provider_call()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var holdExpiry = _harness.Clock.Now.AddMinutes(30);
        _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { ExpiresAt = holdExpiry });
        var reservation = await ReserveAsync(order);
        _harness.Clock.Now = holdExpiry;
        await _harness.EnforceDeadlinesAsync(order);
        var providerCalls = ProviderCalls();

        var result = await _harness.CancelAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Expired, reservation.Status);
        Assert.Equal(providerCalls, ProviderCalls());
        AssertCommitted(order, result);
    }

    [Fact]
    public async Task Unresolved_confirmation_blocks_cancellation()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        await ReserveAsync(order);
        _harness.FlightFlow.ConfirmResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The seat-hold confirmation timed out."));
        await _harness.ConfirmReservedCapacityAsync(order);
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal((2778, savesBefore, 1), (exception.Code, _harness.UnitOfWork.SaveCount, order.CommercialVersion));
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
    }

    [Fact]
    public async Task Unknown_cancellation_outcome_keeps_the_services_active_and_recoverable()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ConfirmAsync(order);
        LoseNextCancellationResponse();

        var result = await _harness.CancelAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal(((long?)null, 1, OrderFulfillmentStatus.Unknown), (result.OrderChangeId, order.CommercialVersion, task.Status));
        Assert.All(order.Services, service => Assert.True(service.IsActive));
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.Equal(FulfillmentFailureReason.UnknownOutcome, result.Reservations.Single().FailureReason);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Retry_after_an_unknown_outcome_replays_the_exact_request_once_then_commits()
    {
        var order = SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);
        var reservation = await ConfirmAsync(order);
        LoseNextCancellationResponse();
        await _harness.CancelAsync(order);

        var result = await _harness.CancelAsync(order);

        var requests = _harness.FlightFlow.CancelConfirmedRequests;
        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal(2, requests.Count);
        Assert.Equal(requests[0].HoldBatchId, requests[1].HoldBatchId);
        Assert.Equal(requests[0].SeatHoldReferences, requests[1].SeatHoldReferences);
        Assert.Equal((OrderFulfillmentStatus.Succeeded, 2), (task.Status, task.AttemptCount));
        Assert.Single(_harness.Tasks.Committed, item => item.TaskType == OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal(task.Interactions.First().RequestPayload, task.Interactions.Last().RequestPayload);
        AssertCommitted(order, result);
    }

    [Fact]
    public async Task Recovery_without_its_original_request_refuses_without_calling_the_provider()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ConfirmAsync(order);
        var orphan = FulfillmentTask.Create(
            _harness.Ids.NewId(),
            order.Id,
            reservation.Id,
            OrderFulfillmentTaskType.CancelConfirmed,
            FulfillmentProviderKeys.FlightFlow,
            "cancel-confirmed:orphan",
            "order:orphan",
            FulfillmentTargetKind.ReservationUnit,
            reservation.Units.Select(unit => unit.Id).ToList(),
            OrderFulfillmentTargetAction.Cancel,
            _harness.Ids,
            _harness.Clock.Now);
        orphan.StartAttempt(_harness.Ids, _harness.Clock.Now);
        await _harness.Tasks.AddAsync(orphan);
        await _harness.UnitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal((2807, 500), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.All(order.Services, service => Assert.True(service.IsActive));
    }

    [Fact]
    public async Task Recovery_refused_without_state_evidence_stays_unknown()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ConfirmAsync(order);
        LoseNextCancellationResponse();
        await _harness.CancelAsync(order);
        _harness.FlightFlow.CancelConfirmedWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1166)));

        var result = await _harness.CancelAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal((OrderFulfillmentStatus.Unknown, true, (long?)null), (task.Status, task.IsResumable, result.OrderChangeId));
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.All(order.Services, service => Assert.True(service.IsActive));
    }

    [Fact]
    public async Task Coded_provider_state_is_applied_without_a_commercial_commit()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        var reservation = await ConfirmAsync(order);
        _harness.FlightFlow.CancelConfirmedWireResponses.Enqueue(FlightFlowWire.Response(HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1188)));

        var result = await _harness.CancelAsync(order);

        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal((FulfillmentReservationStatus.Released, OrderFulfillmentStatus.Failed, (long?)null), (reservation.Status, task.Status, result.OrderChangeId));
        Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.All(order.Services, service => Assert.True(service.IsActive));
    }

    [Fact]
    public async Task One_settled_and_one_unknown_reservation_keep_provider_truth_without_a_commercial_commit()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)], new BoundSpec("OUT", 101), new BoundSpec("RET", 201));
        var outbound = await ConfirmServicesAsync(order, ReservationHarness.AirService(order, 1, 101).Id);
        var inbound = await ConfirmServicesAsync(order, ReservationHarness.AirService(order, 1, 201).Id);
        _harness.FlightFlow.CancelConfirmedResponses.Enqueue(ScriptedFlightFlowProvider.Cancelled);
        LoseNextCancellationResponse();

        var result = await _harness.CancelAsync(order);

        Assert.Equal((FulfillmentReservationStatus.Cancelled, FulfillmentReservationStatus.Unknown), (outbound.Status, inbound.Status));
        Assert.Equal(((long?)null, 1), (result.OrderChangeId, order.CommercialVersion));
        Assert.All(order.Services, service => Assert.True(service.IsActive));

        var retried = await _harness.CancelAsync(order);

        Assert.Equal(3, _harness.FlightFlow.CancelConfirmedRequests.Count);
        Assert.Equal(inbound.ProviderOperationRef, _harness.FlightFlow.CancelConfirmedRequests.Last().HoldBatchId);
        AssertCommitted(order, retried);
    }

    [Fact]
    public async Task Ticketed_scope_requires_document_servicing_before_cancellation()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        await ConfirmAsync(order);
        await _harness.IssueAsync(order, (await _harness.DefineStockAsync()).DocumentStockId);
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal((2803, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal((savesBefore, OrderStatus.Ticketed), (_harness.UnitOfWork.SaveCount, order.Status));
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
    }

    [Fact]
    public async Task Repeated_cancellation_adds_no_change_version_or_task()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        await ConfirmAsync(order);
        var first = await _harness.CancelAsync(order);
        var state = (order.CommercialVersion, order.Changes.Count, _harness.Tasks.Committed.Count, _harness.FlightFlow.CancelConfirmedRequests.Count);

        var repeated = await _harness.CancelAsync(order);

        Assert.Equal(state, (order.CommercialVersion, order.Changes.Count, _harness.Tasks.Committed.Count, _harness.FlightFlow.CancelConfirmedRequests.Count));
        Assert.Equal(((long?)null, OrderStatus.Cancelled, first.CommercialVersion), (repeated.OrderChangeId, repeated.OrderStatus, repeated.CommercialVersion));
    }

    [Fact]
    public async Task Cancelling_one_leg_keeps_its_item_active_until_the_last_leg_ends()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)], new BoundSpec("OUT", 101, 102));
        await ConfirmAsync(order);
        var secondLeg = ReservationHarness.AirService(order, 1, 102);

        await _harness.CancelAsync(order, secondLeg.Id);
        var afterFirst = order.Items.Single().CommercialStatus;
        await _harness.CancelAsync(order);

        Assert.Equal((OrderItemCommercialState.Active, OrderItemCommercialState.Cancelled), (afterFirst, order.Items.Single().CommercialStatus));
        Assert.Equal(3, order.CommercialVersion);
    }

    [Fact]
    public async Task Cancellation_keeps_the_record_locator_and_the_priced_order()
    {
        var order = SeedOrder([TravellerSpec.Adult(1)]);
        await ConfirmAsync(order);
        var kept = (order.RecordLocator, order.CustomerTotal, Lines: order.PricingLines.Count);

        await _harness.CancelAsync(order);

        Assert.NotNull(order.RecordLocator);
        Assert.Equal(kept, (order.RecordLocator, order.CustomerTotal, order.PricingLines.Count));
    }

    [Fact]
    public void Cancellation_has_no_payment_dependency()
        => Assert.DoesNotContain(
            typeof(CancelOrderService).GetConstructors().Single().GetParameters(),
            parameter => parameter.ParameterType.FullName!.Contains("Payment", StringComparison.OrdinalIgnoreCase)
                         || parameter.ParameterType.FullName.Contains("JetPay", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public async Task Infant_only_cancellation_changes_no_provider_capacity()
    {
        var order = SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)]);
        var reservation = await ConfirmAsync(order);
        var infant = ReservationHarness.AirService(order, 2, 101);

        var result = await _harness.CancelAsync(order, infant.Id);

        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
        Assert.Equal(FulfillmentReservationStatus.Confirmed, reservation.Status);
        Assert.Equal([infant.Id], result.CancelledServiceIds);
        Assert.Equal(OrderServiceCommercialState.Cancelled, infant.CommercialStatus);
        Assert.True(ReservationHarness.AirService(order, 1, 101).IsActive);
    }

    [Fact]
    public async Task Order_without_reservation_capacity_is_cancelled_without_a_provider_call()
    {
        var provider = new StubReservationProvider("ProviderN", ReservationMode.None, ReservationMemberStatus.Confirmed);
        var harness = new ReservationHarness(provider);
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, provider.ProviderKey);

        var result = await harness.CancelAsync(order);

        Assert.Empty(provider.CancelConfirmedCalls);
        Assert.Empty(provider.ReleaseCalls);
        Assert.Equal(OrderStatus.Cancelled, result.OrderStatus);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    private Order SeedOrder(IReadOnlyList<TravellerSpec> travellers, params BoundSpec[] bounds)
        => _harness.SeedOrder(travellers, bounds.Length == 0 ? [new BoundSpec("OUT", 101)] : bounds);

    private async Task<FulfillmentReservation> ReserveAsync(Order order)
        => _harness.Reservation((await _harness.ReserveOrderAsync(order)).Reservations.Single().ReservationId);

    private async Task<FulfillmentReservation> ConfirmAsync(Order order)
    {
        var reservation = await ReserveAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);
        return reservation;
    }

    private async Task<FulfillmentReservation> ConfirmServicesAsync(Order order, long serviceId)
    {
        var reservation = _harness.Reservation((await _harness.ReserveServicesAsync(order, serviceId)).Reservations.Single().ReservationId);
        await _harness.ConfirmReservationsAsync(order, reservation.Id);
        return reservation;
    }

    private void LoseNextCancellationResponse()
        => _harness.FlightFlow.CancelConfirmedResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The confirmed-seat cancellation timed out."));

    private (int Holds, int Confirms, int Releases, int Cancels) ProviderCalls()
        => (_harness.FlightFlow.HoldRequests.Count, _harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count, _harness.FlightFlow.CancelConfirmedRequests.Count);

    private static void AssertCommitted(Order order, CancelOrderResult result)
    {
        var change = order.Changes.Single(item => item.ChangeType == OrderChangeType.Cancel && item.Id == result.OrderChangeId);

        Assert.Equal((OrderStatus.Cancelled, order.CommercialVersion), (result.OrderStatus, result.CommercialVersion));
        Assert.Equal(change.CommercialVersion, order.CommercialVersion);
        Assert.All(order.Services, service => Assert.Equal(OrderServiceCommercialState.Cancelled, service.CommercialStatus));
        Assert.All(order.Items, item => Assert.Equal(OrderItemCommercialState.Cancelled, item.CommercialStatus));
        Assert.Equal(nameof(VoidReason.CustomerRequest), change.ReasonCode);
    }
}
