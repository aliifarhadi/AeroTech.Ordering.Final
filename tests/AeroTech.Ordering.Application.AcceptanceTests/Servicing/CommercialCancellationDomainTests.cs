using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class CommercialCancellationDomainTests
{
    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Cancellation_appends_one_cancel_change_at_the_next_commercial_version()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var changesBefore = order.Changes.ToList();

        var change = Cancel(order, order.CancellationScopeOf([]));

        Assert.Equal(changesBefore.Append(change), order.Changes);
        Assert.Equal(
            (OrderChangeType.Cancel, 2, 2, (string?)null, (string?)null, nameof(VoidReason.CustomerRequest), false, (string?)null),
            (change.ChangeType, change.CommercialVersion, order.CommercialVersion, change.SourceReference, change.SourceSystem, change.ReasonCode, change.IsInvoluntary, change.WaiverCode));
        Assert.Equal(ReservationHarness.CancellingActor, change.ActorContext);
    }

    [Fact]
    public void Cancelled_services_record_the_change_that_ended_them()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var change = Cancel(order, order.CancellationScopeOf([]));

        Assert.All(order.Services, service => Assert.Equal((OrderServiceCommercialState.Cancelled, change.Id), (service.CommercialStatus, service.EndedByChangeId!.Value)));
        Assert.All(order.Items, item => Assert.Equal((OrderItemCommercialState.Cancelled, change.Id), (item.CommercialStatus, item.EndedByChangeId!.Value)));
    }

    [Fact]
    public void Item_closes_only_when_its_last_active_service_ends()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        var firstLeg = ReservationHarness.AirService(order, 1, 101);
        var secondLeg = ReservationHarness.AirService(order, 1, 102);
        var item = order.Items.Single();

        Cancel(order, order.CancellationScopeOf([firstLeg.Id]));
        var afterFirst = (item.CommercialStatus, item.EndedByChangeId, secondLeg.CommercialStatus, secondLeg.EndedByChangeId);
        var closing = Cancel(order, order.CancellationScopeOf([secondLeg.Id]));

        Assert.Equal((OrderItemCommercialState.Active, (long?)null, OrderServiceCommercialState.Active, (long?)null), afterFirst);
        Assert.Equal((OrderItemCommercialState.Cancelled, closing.Id), (item.CommercialStatus, item.EndedByChangeId!.Value));
        Assert.Equal(3, order.CommercialVersion);
    }

    [Fact]
    public void Cancellation_preserves_every_row_and_the_priced_totals()
    {
        var order = Create([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var rows = (Services: order.Services.Count, Items: order.Items.Count, Lines: order.PricingLines.Count, Allocations: order.PricingLines.Sum(line => line.Allocations.Count));
        var totals = (order.CustomerTotal, Items: order.Items.Select(item => item.AcceptedTotal).ToList());

        Cancel(order, order.CancellationScopeOf([]));

        Assert.Equal(rows, (order.Services.Count, order.Items.Count, order.PricingLines.Count, order.PricingLines.Sum(line => line.Allocations.Count)));
        Assert.Equal(totals.CustomerTotal, order.CustomerTotal);
        Assert.NotEqual(0m, order.CustomerTotal);
        Assert.Equal(totals.Items, order.Items.Select(item => item.AcceptedTotal));
    }

    [Fact]
    public void Already_cancelled_scope_resolves_to_nothing_new()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var service = ReservationHarness.AirService(order, 1, 101);
        Cancel(order, order.CancellationScopeOf([service.Id]));

        Assert.Empty(order.CancellationScopeOf([service.Id]));
        Assert.Empty(order.CancellationScopeOf([]));
    }

    [Fact]
    public void Committing_an_empty_or_ended_scope_is_refused()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var service = ReservationHarness.AirService(order, 1, 101);
        Cancel(order, [service]);

        var empty = Assert.Throws<BusinessException>(() => order.Cancel([], ReservationHarness.CancellingActor, "CustomerRequest", null, _ids, _clock.Now));
        var ended = Assert.Throws<BusinessException>(() => order.Cancel([service.Id], ReservationHarness.CancellingActor, "CustomerRequest", null, _ids, _clock.Now));

        Assert.Equal((2818, 2733), (empty.Code, ended.Code));
        Assert.Equal(2, order.CommercialVersion);
    }

    [Fact]
    public void Air_cancellation_includes_its_seat()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], [new SeatSpec(1, "OUT", "12A")]);
        var air = ReservationHarness.AirService(order, 1, 101);
        var seat = order.Services.OfType<OrderSeatService>().Single();

        Assert.Equal([air.Id, seat.Id], order.CancellationScopeOf([air.Id]).Select(service => service.Id));
    }

    [Fact]
    public void Adult_cancellation_includes_the_lap_infant_on_the_same_segment_only()
    {
        var order = Create([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var adultOut = ReservationHarness.AirService(order, 1, 101);
        var infantOut = ReservationHarness.AirService(order, 2, 101);

        Assert.Equal([adultOut.Id, infantOut.Id], order.CancellationScopeOf([adultOut.Id]).Select(service => service.Id));
    }

    [Fact]
    public void Infant_only_cancellation_does_not_include_the_parent()
    {
        var order = Create([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101)]);
        var infant = ReservationHarness.AirService(order, 2, 101);

        Assert.Equal([infant.Id], order.CancellationScopeOf([infant.Id]).Select(service => service.Id));
    }

    [Fact]
    public void Seat_only_cancellation_is_deferred_to_the_ancillary_stage()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], [new SeatSpec(1, "OUT", "12A")]);
        var seat = order.Services.OfType<OrderSeatService>().Single();

        var exception = Assert.Throws<BusinessException>(() => order.CancellationScopeOf([seat.Id]));

        Assert.Equal((2804, 422), (exception.Code, exception.HttpStatus));
        Assert.Equal((OrderServiceCommercialState.Active, "12A"), (seat.CommercialStatus, seat.SeatNumber));
    }

    [Fact]
    public void Unknown_service_is_not_found_in_the_order()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var exception = Assert.Throws<BusinessException>(() => order.CancellationScopeOf([404]));

        Assert.Equal((2732, 404), (exception.Code, exception.HttpStatus));
    }

    [Fact]
    public void Servicing_summary_cancels_the_order_only_when_no_active_service_remains()
    {
        var order = Create([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        var firstLeg = ReservationHarness.AirService(order, 1, 101);

        Cancel(order, order.CancellationScopeOf([firstLeg.Id]));
        order.SummarizeServicing(new HashSet<long>(), [], new Dictionary<long, ReservationMemberStatus>());
        var partial = order.Status;
        Cancel(order, order.CancellationScopeOf([]));
        order.SummarizeServicing(new HashSet<long>(), [], new Dictionary<long, ReservationMemberStatus>());

        Assert.Equal((OrderStatus.Created, OrderStatus.Cancelled), (partial, order.Status));
    }

    private Order Create(IReadOnlyList<TravellerSpec> travellers, IReadOnlyList<BoundSpec> bounds, IReadOnlyList<SeatSpec>? seats = null)
        => OrderFixture.Create(_ids, _clock, travellers, bounds, seats);

    private OrderChange Cancel(Order order, IReadOnlyList<OrderService> scope)
        => order.Cancel(scope.Select(service => service.Id).ToList(), ReservationHarness.CancellingActor, nameof(VoidReason.CustomerRequest), null, _ids, _clock.Now);
}
