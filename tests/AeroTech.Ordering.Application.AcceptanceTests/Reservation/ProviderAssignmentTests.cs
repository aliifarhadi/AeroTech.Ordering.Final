using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ProviderAssignmentTests
{
    [Fact]
    public void Air_and_seat_services_are_assigned_to_FlightFlow_at_creation()
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], [new SeatSpec(1, "OUT", "12A")]);

        var seat = order.Services.OfType<OrderSeatService>().Single();
        var air = order.Services.OfType<OrderAirTransportService>().Single(service => service.Id == seat.AssociatedAirServiceId);

        Assert.All(order.Services, service => Assert.Equal(FulfillmentProviderKeys.FlightFlow, service.FulfillmentProviderKey));
        Assert.Equal(air.FulfillmentProviderKey, seat.FulfillmentProviderKey);
    }

    [Fact]
    public void Provider_assignment_is_immutable()
    {
        var setter = typeof(OrderService).GetProperty(nameof(OrderService.FulfillmentProviderKey))!.SetMethod!;

        Assert.False(setter.IsPublic);
        Assert.False(setter.IsAssembly);
        Assert.False(setter.IsFamily);
    }
}
