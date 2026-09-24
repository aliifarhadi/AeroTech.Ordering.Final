using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ProviderGroupingTests
{
    private readonly StubReservationProvider _providerA = new("ProviderA", ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held);
    private readonly StubReservationProvider _providerB = new("ProviderB", ReservationMode.ImmediateConfirm, ReservationMemberStatus.Confirmed);
    private readonly StubReservationProvider _providerC = new("ProviderC", ReservationMode.None, ReservationMemberStatus.Confirmed);

    [Fact]
    public async Task Automatic_reserve_runs_one_operation_per_reservable_provider_and_skips_mode_none()
    {
        var harness = new ReservationHarness(_providerA, _providerB, _providerC);
        var order = harness.SeedOrder(
            [TravellerSpec.Adult(1), TravellerSpec.Adult(2), TravellerSpec.Adult(3)],
            [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, _providerA.ProviderKey);
        ReservationHarness.AssignProvider(order, 2, _providerB.ProviderKey);
        ReservationHarness.AssignProvider(order, 3, _providerC.ProviderKey);

        var result = await harness.ReserveOrderAsync(order);

        Assert.Single(_providerA.ReserveCalls);
        Assert.Single(_providerB.ReserveCalls);
        Assert.Empty(_providerC.ReserveCalls);
        Assert.Empty(harness.FlightFlow.HoldRequests);
        Assert.Equal(["ProviderA", "ProviderB"], result.Reservations.Select(reservation => reservation.FulfillmentProviderKey).Order());
        Assert.Equal(ReservationMode.ImmediateConfirm, harness.Reservation(result.Reservations.Single(item => item.FulfillmentProviderKey == "ProviderB").ReservationId).Mode);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task Order_without_reservable_services_makes_no_supplier_call_and_stays_created()
    {
        var harness = new ReservationHarness(_providerC);
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, _providerC.ProviderKey);

        var result = await harness.ReserveOrderAsync(order);

        Assert.Empty(result.Reservations);
        Assert.Empty(_providerC.ReserveCalls);
        Assert.Empty(harness.Reservations.Committed);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Null(order.RecordLocator);
    }
}
