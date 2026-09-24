using System.Reflection;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class SeatReservationTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Air_and_selected_seat_share_one_provider_unit()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], [new SeatSpec(1, "OUT", "12A")]);
        var seat = order.Services.OfType<OrderSeatService>().Single();

        var result = await _harness.ReserveOrderAsync(order);

        var unit = _harness.Reservation(result.Reservations.Single().ReservationId).Units.Single();
        Assert.Equal([seat.AssociatedAirServiceId, seat.Id], unit.OrderServiceIds.Order());
        Assert.Equal("12A", _harness.FlightFlow.HoldRequests.Single().Flights.Single().Seats.Single().Seat);
        Assert.Equal("12A", unit.ObservedSeat);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Air_without_seat_reserves_without_a_seat_request()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var result = await _harness.ReserveOrderAsync(order);

        Assert.Null(_harness.FlightFlow.HoldRequests.Single().Flights.Single().Seats.Single().Seat);
        Assert.Equal(FulfillmentReservationStatus.Held, result.Reservations.Single().Status);
    }

    [Fact]
    public async Task No_independent_seat_hold_is_created()
    {
        var order = _harness.SeedOrder(
            [TravellerSpec.Adult(1), TravellerSpec.Adult(2)],
            [new BoundSpec("OUT", 101, 102)],
            [new SeatSpec(1, "OUT", "12A"), new SeatSpec(2, "OUT", "12B")]);

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        Assert.Equal(ReservationHarness.AirServices(order).Count, reservation.Units.Count);
        Assert.Equal(order.Services.Count, reservation.CoveredOrderServiceIds.Count);
        Assert.Equal(4, _harness.FlightFlow.HoldRequests.Single().Flights.Sum(flight => flight.Seats.Count));
    }

    [Fact]
    public async Task Seat_that_would_join_an_existing_hold_is_rejected_before_any_provider_call()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], [new SeatSpec(1, "OUT", "12A")]);
        var seat = order.Services.OfType<OrderSeatService>().Single();
        var services = (List<OrderService>)typeof(Order).GetField("_services", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(order)!;

        services.Remove(seat);
        await _harness.ReserveOrderAsync(order);
        services.Add(seat);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveServicesAsync(order, seat.Id));

        Assert.Equal(2735, exception.Code);
        Assert.Single(_harness.FlightFlow.HoldRequests);
        Assert.Single(_harness.Reservations.Committed);
    }
}
