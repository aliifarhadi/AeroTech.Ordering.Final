using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ScopeSafeCancellationRecoveryTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Unresolved_effect_outside_a_new_cancellation_blocks_it_without_any_provider_call()
    {
        var (order, reservation, outbound, inbound) = await OutboundCancellationLostAsync();
        var task = CancelTask(reservation);
        var before = (_harness.UnitOfWork.SaveCount, _harness.FlightFlow.CancelConfirmedRequests.Count, task.AttemptCount, order.CommercialVersion, order.Changes.Count);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, inbound.Id));

        Assert.Equal((2778, 409), (exception.Code, exception.HttpStatus));
        Assert.Equal(before, (_harness.UnitOfWork.SaveCount, _harness.FlightFlow.CancelConfirmedRequests.Count, task.AttemptCount, order.CommercialVersion, order.Changes.Count));
        Assert.Equal((OrderFulfillmentStatus.Unknown, true), (task.Status, task.IsResumable));
        Assert.Equal((ReservationMemberStatus.Unknown, ReservationMemberStatus.Confirmed), (UnitOf(reservation, outbound).Status, UnitOf(reservation, inbound).Status));
        Assert.True(outbound.IsActive && inbound.IsActive);
        Assert.Single(_harness.Tasks.Committed, item => item.TaskType == OrderFulfillmentTaskType.CancelConfirmed);
    }

    [Fact]
    public async Task Exact_retry_replays_the_persisted_request_and_cancels_only_its_scope()
    {
        var (order, reservation, outbound, inbound) = await OutboundCancellationLostAsync();

        var result = await _harness.CancelAsync(order, outbound.Id);

        var requests = _harness.FlightFlow.CancelConfirmedRequests;
        Assert.Equal(2, requests.Count);
        Assert.Equal((requests[0].HoldBatchId, requests[0].ReasonCode), (requests[1].HoldBatchId, requests[1].ReasonCode));
        Assert.Equal([UnitOf(reservation, outbound).ProviderUnitRef!], requests[1].SeatHoldReferences);
        Assert.Single(_harness.Tasks.Committed, item => item.TaskType == OrderFulfillmentTaskType.CancelConfirmed);
        Assert.Equal((OrderServiceCommercialState.Cancelled, OrderServiceCommercialState.Active), (outbound.CommercialStatus, inbound.CommercialStatus));
        Assert.Equal((ReservationMemberStatus.Cancelled, ReservationMemberStatus.Confirmed), (UnitOf(reservation, outbound).Status, UnitOf(reservation, inbound).Status));
        Assert.Equal([outbound.Id], result.CancelledServiceIds);
        Assert.Equal(2, order.CommercialVersion);
    }

    [Fact]
    public async Task Superset_retry_recovers_the_old_effect_then_cancels_only_the_remaining_units_under_a_new_task()
    {
        var (order, reservation, outbound, inbound) = await OutboundCancellationLostAsync();
        var original = CancelTask(reservation).Interactions.Single().RequestPayload;

        var result = await _harness.CancelAsync(order, outbound.Id, inbound.Id);

        var requests = _harness.FlightFlow.CancelConfirmedRequests;
        var tasks = _harness.Tasks.Committed.Where(item => item.TaskType == OrderFulfillmentTaskType.CancelConfirmed).ToList();
        Assert.Equal(3, requests.Count);
        Assert.Equal([UnitOf(reservation, outbound).ProviderUnitRef!], requests[1].SeatHoldReferences);
        Assert.Equal([UnitOf(reservation, inbound).ProviderUnitRef!], requests[2].SeatHoldReferences);
        Assert.Equal(2, tasks.Count);
        Assert.All(tasks[0].Interactions, interaction => Assert.Equal(original, interaction.RequestPayload));
        Assert.Equal([UnitOf(reservation, outbound).Id], tasks[0].Targets.Select(target => target.TargetId));
        Assert.Equal([UnitOf(reservation, inbound).Id], tasks[1].Targets.Select(target => target.TargetId));
        Assert.Single(order.Changes, change => change.ChangeType == OrderChangeType.Cancel);
        Assert.Equal((2, OrderStatus.Cancelled), (order.CommercialVersion, order.Status));
        Assert.Equal(new[] { outbound.Id, inbound.Id }.Order(), result.CancelledServiceIds.Order());
    }

    [Fact]
    public async Task Recovery_scope_is_the_persisted_task_target_not_the_current_reservation_state()
    {
        var (order, reservation, outbound, inbound) = await OutboundCancellationLostAsync();
        typeof(ReservationUnit).GetProperty(nameof(ReservationUnit.Status))!.SetValue(UnitOf(reservation, inbound), ReservationMemberStatus.Unknown);

        var blocked = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, inbound.Id));
        var callsWhenBlocked = _harness.FlightFlow.CancelConfirmedRequests.Count;
        var result = await _harness.CancelAsync(order, outbound.Id);

        Assert.Equal((2778, 1), (blocked.Code, callsWhenBlocked));
        Assert.Equal(2, _harness.FlightFlow.CancelConfirmedRequests.Count);
        Assert.Equal([UnitOf(reservation, outbound).ProviderUnitRef!], _harness.FlightFlow.CancelConfirmedRequests[1].SeatHoldReferences);
        Assert.Equal((ReservationMemberStatus.Cancelled, ReservationMemberStatus.Unknown), (UnitOf(reservation, outbound).Status, UnitOf(reservation, inbound).Status));
        Assert.Equal([outbound.Id], result.CancelledServiceIds);
        Assert.True(inbound.IsActive);
    }

    [Fact]
    public async Task Unresolved_task_without_reservation_unit_targets_is_an_impossible_state()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var reservation = await ConfirmAsync(order);
        var malformed = FulfillmentTask.Create(
            _harness.Ids.NewId(),
            order.Id,
            reservation.Id,
            OrderFulfillmentTaskType.CancelConfirmed,
            FulfillmentProviderKeys.FlightFlow,
            "cancel-confirmed:malformed",
            "order:malformed",
            FulfillmentTargetKind.OrderService,
            [ReservationHarness.AirService(order, 1, 101).Id],
            OrderFulfillmentTargetAction.Cancel,
            _harness.Ids,
            _harness.Clock.Now);
        malformed.StartAttempt(_harness.Ids, _harness.Clock.Now);
        await _harness.Tasks.AddAsync(malformed);
        await _harness.UnitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal((2819, 500), (exception.Code, exception.HttpStatus));
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
    }

    private async Task<(Order Order, FulfillmentReservation Reservation, OrderService Outbound, OrderService Inbound)> OutboundCancellationLostAsync()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var reservation = await ConfirmAsync(order);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var inbound = ReservationHarness.AirService(order, 1, 201);
        _harness.FlightFlow.CancelConfirmedResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The confirmed-seat cancellation timed out."));

        await _harness.CancelAsync(order, outbound.Id);

        var task = CancelTask(reservation);
        Assert.Equal((OrderFulfillmentStatus.Unknown, ReservationMemberStatus.Unknown), (task.Status, UnitOf(reservation, outbound).Status));
        Assert.Equal([UnitOf(reservation, outbound).Id], task.Targets.Select(target => target.TargetId));

        return (order, reservation, outbound, inbound);
    }

    private async Task<FulfillmentReservation> ConfirmAsync(Order order)
    {
        var reservation = _harness.Reservation((await _harness.ReserveOrderAsync(order)).Reservations.Single().ReservationId);
        await _harness.ConfirmReservedCapacityAsync(order);
        return reservation;
    }

    private FulfillmentTask CancelTask(FulfillmentReservation reservation)
        => _harness.Tasks.Committed.First(item => item.FulfillmentReservationId == reservation.Id && item.TaskType == OrderFulfillmentTaskType.CancelConfirmed);

    private static ReservationUnit UnitOf(FulfillmentReservation reservation, OrderService service)
        => reservation.Units.Single(unit => unit.OrderServiceIds.Contains(service.Id));
}
