using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class InfantReservationTests
{
    [Fact]
    public async Task Lap_infant_reservation_is_blocked_until_the_FlightFlow_mapping_is_confirmed()
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101)]);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => harness.ReserveOrderAsync(order));

        Assert.Equal(2736, exception.Code);
        Assert.Equal(501, exception.HttpStatus);
        Assert.Empty(harness.FlightFlow.HoldRequests);
        Assert.Empty(harness.AirFareValidator.Calls);
        Assert.Empty(harness.Reservations.Committed);
        Assert.Equal(OrderStatus.Created, order.Status);
    }
}
