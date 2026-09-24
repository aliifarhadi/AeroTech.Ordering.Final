using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain._Shared.Resources;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class AirFareValidationTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Valid_validation_attempts_the_hold()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        await _harness.ReserveOrderAsync(order);

        Assert.Single(_harness.AirFareValidator.Calls);
        Assert.Single(_harness.FlightFlow.HoldRequests);
    }

    [Fact]
    public async Task Failed_validation_makes_no_hold_call_and_persists_nothing()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.AirFareValidator.Behavior = (_, _) => throw ExceptionFactory.FareReservationCouldNotBeValidated("RBD closed");

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal(2607, exception.Code);
        Assert.Empty(_harness.FlightFlow.HoldRequests);
        Assert.Empty(_harness.Reservations.Committed);
        Assert.Empty(_harness.Tasks.Committed);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public async Task Validation_time_limit_constrains_the_requested_expiry()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var timeLimit = _harness.Clock.Now.AddMinutes(5);
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        var result = await _harness.ReserveOrderAsync(order);

        Assert.Equal(timeLimit, _harness.FlightFlow.HoldRequests.Single().ExpiresAt);
        Assert.Equal(timeLimit, _harness.Reservation(result.Reservations.Single().ReservationId).RequestedExpiresAt);
    }

    [Fact]
    public async Task Requested_expiry_is_the_validation_time_limit_unless_the_last_ticketing_date_is_earlier()
    {
        var unconstrained = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var lastTicketingDate = _harness.Clock.Now.AddMinutes(10);
        var ticketingBound = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 102)], lastTicketingDate: lastTicketingDate);
        var timeLimit = _harness.Clock.Now.AddHours(1);
        _harness.AirFareValidator.Behavior = (_, _) => timeLimit;

        await _harness.ReserveOrderAsync(unconstrained);
        await _harness.ReserveOrderAsync(ticketingBound);

        Assert.Equal(timeLimit, _harness.FlightFlow.HoldRequests[0].ExpiresAt);
        Assert.Equal(lastTicketingDate, _harness.FlightFlow.HoldRequests[1].ExpiresAt);
    }

    [Fact]
    public async Task Validation_covers_only_the_targeted_reservation_scope()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("IN", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);

        await _harness.ReserveServicesAsync(order, outbound.Id);

        Assert.Equal([outbound.Id], _harness.AirFareValidator.Calls.Single());
    }
}
