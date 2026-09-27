using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain._Shared.Resources;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class MultiTargetConfirmationTests
{
    private readonly ReservationHarness _harness = new();

    public enum CrossedDeadline
    {
        LastTicketingDate,
        HoldExpiry,
        ValidationTimeLimit
    }

    [Fact]
    public async Task Lapsed_hold_of_a_later_target_fails_the_command_before_any_confirmation()
    {
        var order = SeedOrder();
        var first = await ReserveAsync(order, 1, validUntil: _harness.Clock.Now.AddHours(1), holdExpiry: _harness.Clock.Now.AddHours(2));
        var second = await ReserveAsync(order, 2, validUntil: _harness.Clock.Now.AddHours(1), holdExpiry: _harness.Clock.Now.AddMinutes(30));
        var savesBefore = _harness.UnitOfWork.SaveCount;

        _harness.Clock.Now = second.ExpiresAt!.Value;
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservationsAsync(order, first.Id, second.Id));

        Assert.Equal(2771, exception.Code);
        AssertNothingWasDispatched(savesBefore, first, second);
    }

    [Fact]
    public async Task Refused_revalidation_of_a_later_target_fails_the_command_before_any_confirmation()
    {
        var order = SeedOrder();
        var first = await ReserveAsync(order, 1, validUntil: _harness.Clock.Now.AddHours(1), holdExpiry: _harness.Clock.Now.AddHours(2));
        var second = await ReserveAsync(order, 2, validUntil: _harness.Clock.Now.AddMinutes(20), holdExpiry: _harness.Clock.Now.AddHours(2));
        var firstValidUntil = first.ReservationValidationTimeLimit;
        var savesBefore = _harness.UnitOfWork.SaveCount;

        _harness.Clock.Now = second.ReservationValidationTimeLimit!.Value;
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FareReservationIsNotPermitted("The fare is no longer valid.");
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ConfirmReservationsAsync(order, first.Id, second.Id));

        Assert.Equal(2608, exception.Code);
        Assert.Equal(3, _harness.AirFareValidator.Calls.Count);
        Assert.Equal(firstValidUntil, first.ReservationValidationTimeLimit);
        AssertNothingWasDispatched(savesBefore, first, second);
    }

    [Theory]
    [InlineData(CrossedDeadline.LastTicketingDate, FulfillmentFailureReason.BusinessRejected)]
    [InlineData(CrossedDeadline.HoldExpiry, FulfillmentFailureReason.HoldExpired)]
    [InlineData(CrossedDeadline.ValidationTimeLimit, FulfillmentFailureReason.ValidationFailed)]
    public async Task Deadline_crossed_during_an_earlier_dispatch_fails_only_the_later_target(
        CrossedDeadline crossed,
        FulfillmentFailureReason failureReason)
    {
        var deadline = _harness.Clock.Now.AddMinutes(30);
        var order = SeedOrder(crossed == CrossedDeadline.LastTicketingDate ? deadline : null);
        var first = await ReserveAsync(order, 1, validUntil: _harness.Clock.Now.AddHours(1), holdExpiry: _harness.Clock.Now.AddHours(2));
        var second = await ReserveAsync(
            order,
            2,
            validUntil: crossed == CrossedDeadline.ValidationTimeLimit ? deadline : _harness.Clock.Now.AddHours(1),
            holdExpiry: crossed == CrossedDeadline.HoldExpiry ? deadline : _harness.Clock.Now.AddHours(2));
        _harness.FlightFlow.ConfirmResponses.Enqueue(request =>
        {
            _harness.Clock.Now = deadline;
            return ScriptedFlightFlowProvider.Confirmed(request);
        });

        var result = await _harness.ConfirmReservationsAsync(order, first.Id, second.Id);

        var rejection = crossed switch
        {
            CrossedDeadline.LastTicketingDate => ExceptionFactory.ReservationCannotBeConfirmedAfterLastTicketingDate(second.Id, order.LastTicketingDate),
            CrossedDeadline.HoldExpiry => ExceptionFactory.ReservationHoldHasLapsed(second.Id, second.ExpiresAt),
            _ => ExceptionFactory.ReservationValidationIsStale(second.Id, second.ReservationValidationTimeLimit)
        };
        Assert.Equal(
            [
                (first.Id, FulfillmentReservationStatus.Confirmed, (FulfillmentFailureReason?)null, (string?)null),
                (second.Id, FulfillmentReservationStatus.Held, failureReason, rejection.Message)
            ],
            result.Reservations.Select(item => (item.ReservationId, item.Status, item.FailureReason, item.Error)));
        Assert.Equal(first.ProviderOperationRef, Assert.Single(_harness.FlightFlow.ConfirmRequests).HoldId);
        Assert.Equal(first.Id, Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory).FulfillmentReservationId);
        Assert.All(second.Units, unit => Assert.Equal(ReservationMemberStatus.Held, unit.Status));
        Assert.Equal((OrderStatus.ReservationUnconfirmed, OrderStatus.ReservationUnconfirmed), (result.Status, order.Status));
    }

    private void AssertNothingWasDispatched(int savesBefore, params FulfillmentReservation[] reservations)
    {
        Assert.Equal(savesBefore, _harness.UnitOfWork.SaveCount);
        Assert.Empty(_harness.FlightFlow.ConfirmRequests);
        Assert.DoesNotContain(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.ConfirmInventory);
        Assert.All(reservations, reservation => Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status));
    }

    private Order SeedOrder(DateTimeOffset? lastTicketingDate = null)
        => _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);

    private async Task<FulfillmentReservation> ReserveAsync(Order order, int travellerIndex, DateTimeOffset validUntil, DateTimeOffset holdExpiry)
    {
        _harness.AirFareValidator.Behavior = (_, _) => validUntil;
        _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { ExpiresAt = holdExpiry });

        var result = await _harness.ReserveServicesAsync(order, ReservationHarness.AirService(order, travellerIndex, 101).Id);

        return _harness.Reservation(result.Reservations.Single().ReservationId);
    }
}
