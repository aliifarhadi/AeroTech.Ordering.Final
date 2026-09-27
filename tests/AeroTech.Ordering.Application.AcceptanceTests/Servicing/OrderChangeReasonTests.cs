using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class OrderChangeReasonTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Backoffice_reason_detail_is_kept_trimmed_as_the_cancel_change_reason_text()
    {
        var (order, outbound) = await ConfirmedRoundTripAsync();

        await _harness.CancelThroughBackofficeAsync(order, "  customer changed plan  ", outbound.Id);

        var change = order.Changes.Single(item => item.ChangeType == OrderChangeType.Cancel);
        Assert.Equal((nameof(VoidReason.CustomerRequest), "customer changed plan"), (change.ReasonCode, change.ReasonText));
        Assert.Equal("customer changed plan", _harness.Synchronizer.ServicingProjections.Last().Changes.Single(item => item.ChangeId == change.Id).ReasonText);
    }

    [Fact]
    public async Task Whitespace_reason_detail_is_kept_as_no_reason_text()
    {
        var (order, outbound) = await ConfirmedRoundTripAsync();

        await _harness.CancelThroughBackofficeAsync(order, "   ", outbound.Id);

        Assert.Null(order.Changes.Single(item => item.ChangeType == OrderChangeType.Cancel).ReasonText);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t ", null)]
    [InlineData(" seat no longer needed ", "seat no longer needed")]
    public void Order_change_normalizes_its_reason_text(string? reasonText, string? expected)
    {
        var order = OrderFixture.Create(_harness.Ids, _harness.Clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var change = Cancel(order, reasonText);

        Assert.Equal(expected, change.ReasonText);
    }

    [Fact]
    public void Reason_text_of_exactly_five_hundred_characters_is_accepted()
    {
        var order = OrderFixture.Create(_harness.Ids, _harness.Clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var reasonText = new string('r', 500);

        Assert.Equal(reasonText, Cancel(order, $"  {reasonText}  ").ReasonText);
    }

    [Fact]
    public void Reason_text_over_five_hundred_characters_is_refused_without_changing_the_order()
    {
        var order = OrderFixture.Create(_harness.Ids, _harness.Clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var exception = Assert.Throws<BusinessException>(() => Cancel(order, new string('r', 501)));

        Assert.Equal((2821, 422), (exception.Code, exception.HttpStatus));
        Assert.Equal((1, 1), (order.CommercialVersion, order.Changes.Count));
        Assert.True(ReservationHarness.AirService(order, 1, 101).IsActive);
    }

    private OrderChange Cancel(Order order, string? reasonText)
        => order.Cancel(
            [ReservationHarness.AirService(order, 1, 101).Id],
            ReservationHarness.CancellingActor,
            nameof(VoidReason.CustomerRequest),
            reasonText,
            _harness.Ids,
            _harness.Clock.Now);

    private async Task<(Order Order, OrderService Outbound)> ConfirmedRoundTripAsync()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        return (order, ReservationHarness.AirService(order, 1, 101));
    }
}
