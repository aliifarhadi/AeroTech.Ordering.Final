using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ReservationDeadlineTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Provider_hold_expiring_before_the_validation_time_limit_expires_the_reservation_but_not_the_order()
    {
        var order = SeedOrder();
        var holdExpiry = _harness.Clock.Now.AddMinutes(30);
        var reservation = await ReserveAsync(order, timeLimit: _harness.Clock.Now.AddHours(1), holdExpiry);

        _harness.Clock.Now = holdExpiry;

        Assert.False(reservation.IsConfirmableAt(_harness.Clock.Now, order.LastTicketingDate));
        Assert.Contains(order.Id, await _harness.DueOrderIdsAsync());

        await _harness.EnforceDeadlinesAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Expired, reservation.Status);
        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);

        var renewed = await _harness.ReserveOrderAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Held, renewed.Reservations.Single().Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Validation_time_limit_before_the_provider_hold_releases_the_hold_and_expires_the_order()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: _harness.Clock.Now.AddMinutes(90));

        _harness.Clock.Now = timeLimit;

        Assert.False(reservation.IsConfirmableAt(_harness.Clock.Now, order.LastTicketingDate));
        Assert.Contains(order.Id, await _harness.DueOrderIdsAsync());

        await _harness.EnforceDeadlinesAsync(order);

        Assert.Equal(reservation.ProviderOperationRef, _harness.FlightFlow.ReleaseRequests.Single().HoldId);
        Assert.Equal(FulfillmentReservationStatus.Released, reservation.Status);
        Assert.Equal((OrderStatus.Expired, 1), (order.Status, order.CommercialVersion));
        Assert.Single(_harness.Reservations.Committed);
        Assert.Equal(2731, (await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order))).Code);
    }

    [Fact]
    public async Task Services_past_their_validation_time_limit_cannot_be_reserved_again()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var timeLimit = _harness.Clock.Now.AddHours(1);
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;
        await _harness.ReserveServicesAsync(order, outbound.Id);

        _harness.Clock.Now = timeLimit;
        await _harness.EnforceDeadlinesAsync(order);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveServicesAsync(order, outbound.Id));

        Assert.Equal(2762, exception.Code);
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);
        Assert.Single(_harness.FlightFlow.HoldRequests);
    }

    [Fact]
    public async Task Last_ticketing_date_before_the_validation_time_limit_bounds_the_hold_and_confirmation()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var order = SeedOrder(lastTicketingDate);
        var reservation = await ReserveAsync(order, timeLimit);

        Assert.Equal(lastTicketingDate, _harness.FlightFlow.HoldRequests.Single().ExpiresAt);
        Assert.Equal(timeLimit, reservation.ReservationValidationTimeLimit);
        Assert.True(reservation.IsConfirmableAt(lastTicketingDate.AddSeconds(-1), order.LastTicketingDate));
        Assert.False(reservation.IsConfirmableAt(lastTicketingDate, order.LastTicketingDate));
    }

    [Fact]
    public async Task No_new_reservation_starts_once_the_last_ticketing_date_passed()
    {
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(30);
        var order = SeedOrder(lastTicketingDate);

        _harness.Clock.Now = lastTicketingDate;
        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal(2761, exception.Code);
        Assert.Empty(_harness.AirFareValidator.Calls);
        Assert.Empty(_harness.FlightFlow.HoldRequests);
        Assert.Contains(order.Id, await _harness.DueOrderIdsAsync());

        await _harness.EnforceDeadlinesAsync(order);

        Assert.Equal(OrderStatus.Expired, order.Status);
    }

    [Fact]
    public async Task Validation_time_limit_before_the_last_ticketing_date_bounds_the_hold_and_confirmation()
    {
        var timeLimit = _harness.Clock.Now.AddMinutes(30);
        var order = SeedOrder(_harness.Clock.Now.AddHours(1));
        var reservation = await ReserveAsync(order, timeLimit);

        Assert.Equal(timeLimit, _harness.FlightFlow.HoldRequests.Single().ExpiresAt);
        Assert.True(reservation.IsConfirmableAt(timeLimit.AddSeconds(-1), order.LastTicketingDate));
        Assert.False(reservation.IsConfirmableAt(timeLimit, order.LastTicketingDate));
    }

    [Fact]
    public async Task Earlier_provider_expiry_is_persisted_and_ends_eligibility_at_that_time()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var returned = _harness.Clock.Now.AddMinutes(20);
        var reservation = await ReserveAsync(order, timeLimit, returned);

        Assert.Equal((timeLimit, returned), (reservation.RequestedExpiresAt, reservation.ExpiresAt));
        Assert.True(reservation.IsConfirmableAt(returned.AddSeconds(-1), order.LastTicketingDate));
        Assert.False(reservation.IsConfirmableAt(returned, order.LastTicketingDate));
    }

    [Fact]
    public async Task Later_provider_expiry_is_persisted_but_does_not_extend_the_validation_time_limit()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var returned = _harness.Clock.Now.AddHours(2);
        var reservation = await ReserveAsync(order, timeLimit, returned);

        Assert.Equal((timeLimit, returned, timeLimit), (reservation.RequestedExpiresAt, reservation.ExpiresAt, reservation.ReservationValidationTimeLimit));
        Assert.False(reservation.HoldLapsedAt(timeLimit));
        Assert.False(reservation.IsConfirmableAt(timeLimit, order.LastTicketingDate));
    }

    [Fact]
    public async Task Unknown_release_at_the_deadline_is_reconciled_before_the_order_expires()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        var reservation = await ReserveAsync(order, timeLimit, holdExpiry: _harness.Clock.Now.AddMinutes(90));
        _harness.Clock.Now = timeLimit;
        _harness.FlightFlow.ReleaseResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));

        await _harness.EnforceDeadlinesAsync(order);

        Assert.Equal((FulfillmentReservationStatus.Held, OrderStatus.Confirmed), (reservation.Status, order.Status));

        await _harness.EnforceDeadlinesAsync(order);

        var releaseInteractions = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReleaseReserved).Interactions.ToList();

        Assert.Equal(2, _harness.FlightFlow.ReleaseRequests.Count);
        Assert.Single(releaseInteractions.Select(interaction => (interaction.RequestPayload, interaction.IdempotencyKey)).Distinct());
        Assert.Equal((FulfillmentReservationStatus.Released, OrderStatus.Expired), (reservation.Status, order.Status));
    }

    [Fact]
    public async Task Unknown_reservation_at_the_deadline_is_neither_released_nor_expired()
    {
        var order = SeedOrder();
        var timeLimit = _harness.Clock.Now.AddHours(1);
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));
        var result = await _harness.ReserveOrderAsync(order);

        _harness.Clock.Now = timeLimit.AddMinutes(1);
        await _harness.EnforceDeadlinesAsync(order);

        Assert.Empty(_harness.FlightFlow.ReleaseRequests);
        Assert.Equal(FulfillmentReservationStatus.Unknown, _harness.Reservation(result.Reservations.Single().ReservationId).Status);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Stale_hold_expiry_never_turns_an_issued_order_into_expired()
    {
        var order = SeedOrder();
        var holdExpiry = _harness.Clock.Now.AddMinutes(30);
        var reservation = await ReserveAsync(order, _harness.Clock.Now.AddHours(1), holdExpiry);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Ticketed);

        _harness.Clock.Now = holdExpiry;
        await _harness.EnforceDeadlinesAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Expired, reservation.Status);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    private Order SeedOrder(DateTimeOffset? lastTicketingDate = null)
        => _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], lastTicketingDate: lastTicketingDate);

    private async Task<FulfillmentReservation> ReserveAsync(Order order, DateTimeOffset timeLimit, DateTimeOffset? holdExpiry = null)
    {
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        if (holdExpiry is { } expiresAt)
            _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.Held(request) with { ExpiresAt = expiresAt });

        var result = await _harness.ReserveOrderAsync(order);

        return _harness.Reservation(result.Reservations.Single().ReservationId);
    }
}
