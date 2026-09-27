using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Services.Issuance;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class IssuanceScopeTests
{
    private readonly ReservationHarness _harness = new();
    private readonly IssuancePlanner _planner;

    public IssuanceScopeTests() => _planner = new IssuancePlanner(new ReservationProviderResolver([_harness.FlightFlowReservation]));

    [Fact]
    public async Task Mixed_root_yields_the_confirmed_target_unit_and_its_service()
    {
        var (order, outbound, inbound) = await ConfirmedAsync();
        await _harness.CancelAsync(order, inbound.Id);
        var reservation = _harness.Reservations.Committed.Single();

        var scope = _planner.ScopeOf([outbound], _harness.Reservations.Committed);

        var reservationScope = Assert.Single(scope.ReservationScopes);
        Assert.Equal(FulfillmentReservationStatus.Mixed, reservation.Status);
        Assert.Same(reservation, reservationScope.Reservation);
        Assert.Equal([UnitOf(reservation.Units, outbound).Id], reservationScope.ReservationUnitIds);
        Assert.Equal([outbound.Id], reservationScope.OrderServiceIds);
    }

    [Theory]
    [InlineData(ReservationMemberStatus.Pending)]
    [InlineData(ReservationMemberStatus.Held)]
    [InlineData(ReservationMemberStatus.Waitlisted)]
    [InlineData(ReservationMemberStatus.Unknown)]
    [InlineData(ReservationMemberStatus.Rejected)]
    [InlineData(ReservationMemberStatus.Released)]
    [InlineData(ReservationMemberStatus.Expired)]
    [InlineData(ReservationMemberStatus.Cancelled)]
    public async Task Target_unit_that_is_not_confirmed_is_refused_whatever_the_root_status(ReservationMemberStatus status)
    {
        var (_, outbound, _) = await ConfirmedAsync();
        var reservation = _harness.Reservations.Committed.Single();
        typeof(ReservationUnit).GetProperty(nameof(ReservationUnit.Status))!.SetValue(UnitOf(reservation.Units, outbound), status);

        var exception = Assert.Throws<BusinessException>(() => _planner.ScopeOf([outbound], _harness.Reservations.Committed));

        Assert.Equal((2777, 409), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public async Task Latest_covering_unit_decides_even_when_an_older_reservation_covered_the_service()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var released = await _harness.ReserveOrderAsync(order);
        await _harness.ReleaseAsync(order, released.Reservations.Single().ReservationId);
        var renewed = await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        var scope = _planner.ScopeOf([outbound], _harness.Reservations.Committed);

        Assert.Equal(renewed.Reservations.Single().ReservationId, Assert.Single(scope.ReservationScopes).Reservation.Id);
    }

    private async Task<(Order Order, OrderAirTransportService Outbound, OrderAirTransportService Inbound)> ConfirmedAsync()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        return (order, ReservationHarness.AirService(order, 1, 101), ReservationHarness.AirService(order, 1, 201));
    }

    private static ReservationUnit UnitOf(IEnumerable<ReservationUnit> units, OrderService service)
        => units.Single(unit => unit.OrderServiceIds.Contains(service.Id));
}
