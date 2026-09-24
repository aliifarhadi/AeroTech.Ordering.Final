using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.Providers;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class MultiProviderTruthTests
{
    [Theory]
    [InlineData(ReservationMemberStatus.Rejected, FulfillmentReservationStatus.Rejected)]
    [InlineData(ReservationMemberStatus.Unknown, FulfillmentReservationStatus.Unknown)]
    public async Task One_provider_success_is_preserved_when_another_does_not_succeed(
        ReservationMemberStatus otherOutcome,
        FulfillmentReservationStatus otherStatus)
    {
        var other = new StubReservationProvider("ProviderB", ReservationMode.ImmediateConfirm, otherOutcome);
        var harness = new ReservationHarness(other);
        var order = harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 2, other.ProviderKey);

        var result = await harness.ReserveOrderAsync(order);

        var flightFlow = result.Reservations.Single(item => item.FulfillmentProviderKey == FulfillmentProviderKeys.FlightFlow);
        var provider = result.Reservations.Single(item => item.FulfillmentProviderKey == other.ProviderKey);

        Assert.Equal(FulfillmentReservationStatus.Held, harness.Reservation(flightFlow.ReservationId).Status);
        Assert.Equal(otherStatus, harness.Reservation(provider.ReservationId).Status);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
        Assert.NotNull(order.RecordLocator);
        Assert.Empty(harness.FlightFlow.ReleaseRequests);
        Assert.Empty(other.ReleaseCalls);
    }
}
