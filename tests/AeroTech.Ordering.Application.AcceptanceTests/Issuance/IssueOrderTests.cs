using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Offer;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class IssueOrderTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task One_adult_on_one_flight_is_ticketed_with_one_coupon()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync(rangeFrom: 7);

        var result = await _harness.IssueAsync(order, stock.DocumentStockId);

        var ticket = Assert.Single(_harness.Tickets.Committed);
        var coupon = Assert.Single(ticket.Coupons);
        var service = ReservationHarness.AirService(order, 1, 101);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
        Assert.Equal((OrderStatus.Ticketed, ticket.Id, "09910000000007"), (result.OrderStatus, result.Tickets.Single().ElectronicTicketId, ticket.DocumentNumber));
        Assert.Equal((service.Id, service.SegmentId, 1, 100m), (coupon.OriginalOrderServiceId, coupon.OrderSegmentId, coupon.CouponNumber, coupon.IssuanceValue));
        Assert.Equal((100m, 1, service.TravellerId), (ticket.IssuedTotal, ticket.CurrencyId, ticket.TravellerId));
        Assert.Equal(order.Travellers.Single().CurrentProfileRevisionId, ticket.TravellerProfileRevisionId);
        Assert.Equal(8L, _harness.Stocks.Committed.Single().NextNumber);
    }

    [Fact]
    public async Task One_traveller_on_two_segments_gets_one_ticket_with_ordered_coupons()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", 201)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var ticket = Assert.Single(_harness.Tickets.Committed);
        Assert.Equal(
            [(1, 101L), (2, 102L), (3, 201L)],
            ticket.Coupons.Select(coupon => (coupon.CouponNumber, FlightOf(order, coupon.OrderSegmentId))));
    }

    [Fact]
    public async Task Two_travellers_get_two_tickets_with_independent_numbers()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var tickets = _harness.Tickets.Committed;
        Assert.Equal(["09910000000001", "09910000000002"], tickets.Select(ticket => ticket.DocumentNumber));
        Assert.Equal(order.Travellers.OrderBy(traveller => traveller.Index).Select(traveller => traveller.Id), tickets.Select(ticket => ticket.TravellerId));
        Assert.All(tickets, ticket => Assert.All(ticket.Coupons, coupon => Assert.Equal(ticket.TravellerId, ServiceOf(order, coupon).TravellerId)));
        Assert.Equal(tickets[0].IssuanceContext, tickets[1].IssuanceContext);
        Assert.NotSame(tickets[0].IssuanceContext, tickets[1].IssuanceContext);
    }

    [Fact]
    public async Task Lap_infant_gets_its_own_ticket_without_consuming_a_seat()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Single(_harness.FlightFlow.HoldRequests.Single().Flights.Single().Seats);
        Assert.Equal(2, _harness.Tickets.Committed.Count);
        Assert.Equal(
            order.Services.OfType<OrderAirTransportService>().Select(service => service.Id).Order(),
            _harness.Tickets.Committed.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.OriginalOrderServiceId).Order());
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Repeated_issue_returns_the_same_documents_without_a_new_effect()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        var first = await _harness.IssueAsync(order, stock.DocumentStockId);
        var evidence = Evidence();

        var repeated = await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(evidence, Evidence());
        Assert.Equal(first.IssueFulfillmentTaskId, repeated.IssueFulfillmentTaskId);
        Assert.Equal(first.Tickets.Select(ticket => (ticket.ElectronicTicketId, ticket.DocumentNumber)), repeated.Tickets.Select(ticket => (ticket.ElectronicTicketId, ticket.DocumentNumber)));
    }

    [Fact]
    public async Task Retry_after_a_lost_response_returns_the_committed_documents()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);
        var committed = _harness.Tickets.Committed.Single();

        var retried = await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal((committed.Id, committed.DocumentNumber), (retried.Tickets.Single().ElectronicTicketId, retried.Tickets.Single().DocumentNumber));
        Assert.Single(_harness.Tickets.Committed);
        Assert.Equal(2L, _harness.Stocks.Committed.Single().NextNumber);
    }

    [Fact]
    public async Task Price_links_are_exactly_the_accepted_service_allocations()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101, 102)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        foreach (var coupon in _harness.Tickets.Committed.SelectMany(ticket => ticket.Coupons))
        {
            var expected = order.PricingLines
                .Where(line => line.Treatment == PricingLineTreatment.CustomerPrice)
                .SelectMany(line => line.Allocations.Where(allocation => allocation.OrderServiceId == coupon.OriginalOrderServiceId)
                    .Select(allocation => (line.Id, (long?)allocation.Id, allocation.EquivalentAmount)));

            Assert.Equal(expected, LinksOf(coupon).Select(link => (link.PricingLineId, link.PricingAllocationId, link.AttributedValue)));
        }
    }

    [Fact]
    public async Task Tax_on_a_coupon_is_linked_through_its_pricing_line()
    {
        var offer = WithCoupons(Offer([TravellerSpec.Adult(1)]), coupon => coupon with
        {
            PriceLines = [.. coupon.PriceLines, new OfferPriceLine(OfferPriceCategory.Tax, "Fuel", "YQ", "TAX-1", 20m, 1, 20m, 1, null)]
        });
        var order = await ConfirmedOrderAsync(offer, [TravellerSpec.Adult(1)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var coupon = _harness.Tickets.Committed.Single().Coupons.Single();
        var lines = LinksOf(coupon).Select(link => order.PricingLines.Single(line => line.Id == link.PricingLineId)).ToList();
        Assert.Equal(120m, coupon.IssuanceValue);
        Assert.Contains(lines, line => (line.Category, line.Code, line.Reference) == (OrderPricingLineCategory.Tax, "YQ", "TAX-1"));
    }

    [Fact]
    public async Task Order_scoped_fee_without_allocation_is_not_guessed_onto_a_ticket()
    {
        var offer = Offer([TravellerSpec.Adult(1)]) with
        {
            OrderCharges = [new OfferPriceLine(OfferPriceCategory.Fee, "Service fee", "SF", null, 15m, 1, 15m, 1, null)]
        };
        var order = await ConfirmedOrderAsync(offer, [TravellerSpec.Adult(1)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var ticket = _harness.Tickets.Committed.Single();
        var fee = order.PricingLines.Single(line => line.Code == "SF");
        Assert.Equal((115m, 100m), (order.CustomerTotal, ticket.IssuedTotal));
        Assert.DoesNotContain(ticket.PriceLinks, link => link.PricingLineId == fee.Id);
    }

    [Fact]
    public async Task Fare_booking_class_rbd_and_cabin_snapshots_are_exact()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var coupon = _harness.Tickets.Committed.Single().Coupons.Single();
        var service = ReservationHarness.AirService(order, 1, 101);
        var segment = order.Segments.Single(item => item.Id == service.SegmentId);
        Assert.Equal(
            (service.FareBasis, service.BookingClass, service.RbdId, service.CabinClassId),
            (coupon.FareBasisSnapshot, coupon.BookingClassSnapshot, coupon.RbdIdSnapshot, coupon.CabinClassIdSnapshot));
        Assert.Equal(
            (segment.MarketingAirlineId, segment.OperatingAirlineId, segment.FlightNumber, segment.OriginAirportId, segment.DestinationAirportId, segment.SoldDeparture, segment.SoldArrival),
            (coupon.IssuedSegment.MarketingAirlineId, coupon.IssuedSegment.OperatingAirlineId, coupon.IssuedSegment.FlightNumber, coupon.IssuedSegment.OriginAirportId, coupon.IssuedSegment.DestinationAirportId, coupon.IssuedSegment.DepartureDateTime, coupon.IssuedSegment.ArrivalDateTime));
        Assert.Equal((service.BookingClass, service.RbdId, service.CabinClassId), (coupon.IssuedSegment.BookingClass, coupon.IssuedSegment.RbdId, coupon.IssuedSegment.CabinClassId));
        Assert.Null(coupon.IssuedSegment.SourceSegmentReference);
    }

    [Fact]
    public async Task Checked_baggage_is_snapshotted_and_cabin_baggage_is_not_merged()
    {
        var offer = WithCoupons(Offer([TravellerSpec.Adult(1)]), coupon => coupon with
        {
            CheckedBaggage = new OfferBaggage(2, 23m, "Kg"),
            CabinBaggage = new OfferBaggage(1, 7m, "Kg")
        });
        var order = await ConfirmedOrderAsync(offer, [TravellerSpec.Adult(1)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var coupon = _harness.Tickets.Committed.Single().Coupons.Single();

        Assert.Equal(new BaggageAllowance(2, 23m, WeightUnit.Kg), coupon.BaggageAllowanceSnapshot);
        Assert.NotSame(((OrderAirTransportService)ServiceOf(order, coupon)).CheckedBaggageAllowance, coupon.BaggageAllowanceSnapshot);
    }

    [Fact]
    public async Task Coupon_links_the_unique_accepted_fare_component_of_its_service()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.All(_harness.Tickets.Committed.Single().Coupons, coupon => Assert.Equal(
            order.FarePricingUnits.SelectMany(unit => unit.FareComponents).Single(component => component.CoveredOrderServiceIds.Contains(coupon.OriginalOrderServiceId)).Id,
            coupon.OrderFareComponentId));
    }

    [Fact]
    public async Task Issue_task_is_a_succeeded_local_task_with_typed_targets_and_no_interaction()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();

        var result = await _harness.IssueAsync(order, stock.DocumentStockId);

        var task = _harness.Tasks.Committed.Single(item => item.TaskType == OrderFulfillmentTaskType.IssueTicket);
        var tickets = _harness.Tickets.Committed;
        Assert.Equal(result.IssueFulfillmentTaskId, task.Id);
        Assert.Equal(
            (FulfillmentProviderKeys.LocalDocumentAuthority, $"issue-ticket:{task.Id}", $"order:{order.Id}:issue:{task.Id}", (long?)null),
            (task.FulfillmentProviderKey, task.IdempotencyKey, task.CorrelationReference, task.FulfillmentReservationId));
        Assert.Equal((OrderFulfillmentStatus.Succeeded, 1, 0), (task.Status, task.AttemptCount, task.Interactions.Count));
        Assert.All(task.Targets, target => Assert.Equal(OrderFulfillmentTargetAction.Issue, target.Action));
        Assert.Equal(TargetIds(task, FulfillmentTargetKind.OrderService), ReservationHarness.AirServices(order).Select(service => service.Id).Order());
        Assert.Equal(TargetIds(task, FulfillmentTargetKind.ElectronicTicket), tickets.Select(ticket => ticket.Id).Order());
        Assert.Equal(TargetIds(task, FulfillmentTargetKind.TicketCoupon), tickets.SelectMany(ticket => ticket.Coupons).Select(coupon => coupon.Id).Order());
        Assert.Equal(TargetIds(task, FulfillmentTargetKind.DocumentStockAllocation), _harness.Stocks.Committed.Single().Allocations.Select(allocation => allocation.Id).Order());
        Assert.All(_harness.Stocks.Committed.Single().Allocations, allocation => Assert.Equal((StockNumberState.Issued, task.Id), (allocation.State, allocation.IssueFulfillmentTaskId)));
        Assert.All(tickets, ticket => Assert.Equal(task.Id, ticket.IssueFulfillmentTaskId));
    }

    [Fact]
    public async Task Issue_calls_neither_flight_flow_nor_any_payment_path()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        var flightFlowCalls = (_harness.FlightFlow.HoldRequests.Count, _harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count);

        await _harness.IssueAsync(order, stock.DocumentStockId);

        Assert.Equal(flightFlowCalls, (_harness.FlightFlow.HoldRequests.Count, _harness.FlightFlow.ConfirmRequests.Count, _harness.FlightFlow.ReleaseRequests.Count));
        Assert.Equal(FulfillmentReservationStatus.Confirmed, _harness.Reservations.Committed.Single().Status);
        Assert.Equal([OrderStatus.Ticketed], _harness.Synchronizer.IssueProjections.Select(snapshot => snapshot.Status));
        Assert.Equal(
            [OrderFulfillmentTaskType.ReserveInventory, OrderFulfillmentTaskType.ConfirmInventory, OrderFulfillmentTaskType.IssueTicket],
            _harness.Tasks.Committed.Select(task => task.TaskType));
    }

    [Fact]
    public async Task Seat_assignment_gets_no_coupon_and_no_miscellaneous_document()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)], seats: [new SeatSpec(1, "OUT", "12A")]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var seatService = order.Services.OfType<OrderSeatService>().Single();
        var coupon = _harness.Tickets.Committed.Single().Coupons.Single();
        Assert.NotEqual(seatService.Id, coupon.OriginalOrderServiceId);
        Assert.Equal(2L, _harness.Stocks.Committed.Single().NextNumber);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public void Ticket_model_carries_no_synthesized_emd_reason_for_issuance()
    {
        var members = new[] { typeof(ElectronicTicket), typeof(TicketCoupon), typeof(TicketPriceLink), typeof(IssueTicketCouponArgs) }
            .SelectMany(type => type.GetProperties())
            .Select(property => property.Name);

        Assert.DoesNotContain(members, name => name.Contains("Rfic", StringComparison.OrdinalIgnoreCase)
                                               || name.Contains("Rfisc", StringComparison.OrdinalIgnoreCase)
                                               || name.Contains("ReasonForIssuance", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(Order).Assembly.GetTypes(), type => type.Name is "OrderIssued" or "ElectronicMiscDocument");
    }

    [Fact]
    public async Task Issue_response_mirrors_the_committed_documents()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);
        var stock = await _harness.DefineStockAsync();

        var result = await _harness.IssueAsync(order, stock.DocumentStockId);

        var ticket = _harness.Tickets.Committed.Single();
        var summary = result.Tickets.Single();
        Assert.Equal(
            (ticket.Id, ticket.TravellerId, ticket.DocumentNumber, ticket.StatusSummary, ticket.IssuedAt, ticket.IssuedTotal, ticket.CurrencyId),
            (summary.ElectronicTicketId, summary.TravellerId, summary.DocumentNumber, summary.Status, summary.IssuedAt, summary.IssuedTotal, summary.CurrencyId));
        Assert.Equal(
            ticket.Coupons.Select(coupon => (coupon.Id, coupon.CouponNumber, coupon.OriginalOrderServiceId, coupon.OrderSegmentId, coupon.FinancialStatus, coupon.ControlStatus, coupon.IssuanceValue)),
            summary.Coupons.Select(coupon => (coupon.TicketCouponId, coupon.CouponNumber, coupon.OrderServiceId, coupon.OrderSegmentId, coupon.FinancialStatus, coupon.ControlStatus, coupon.IssuanceValue)));
        Assert.Equal((order.Id, OrderStatus.Ticketed), (result.OrderId, result.OrderStatus));
    }

    [Fact]
    public async Task Issue_context_names_the_stock_owner_and_the_issuing_actor_not_the_sale_actor()
    {
        var order = await ConfirmedOrderAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync(ownerAirlineId: 12, officeId: 34);

        await _harness.IssueAsync(order, stock.DocumentStockId);

        var context = _harness.Tickets.Committed.Single().IssuanceContext;
        Assert.Equal(
            (12, (int?)null, (long?)34, (long?)ReservationHarness.IssueActorId, order.SalesContext.TravelAgencyId, (SalesChannel?)order.SalesContext.Channel),
            (context.IssuerCarrierId, context.ValidatingCarrierId, context.IssuingOfficeId, context.IssuedByActorId, context.TravelAgencyId, context.SalesChannel));
        Assert.NotEqual(order.SalesContext.ActorId, context.IssuedByActorId);
        Assert.Null(context.AgencyIataNumber);
        Assert.Null(context.Pcc);
        Assert.Null(context.SourceFormOfPaymentCode);
    }

    private async Task<Order> ConfirmedOrderAsync(
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<SeatSpec>? seats = null)
    {
        var order = _harness.SeedOrder(travellers, bounds, seats);
        await ReserveAndConfirmAsync(order);
        return order;
    }

    private async Task<Order> ConfirmedOrderAsync(OfferDetail offer, IReadOnlyList<TravellerSpec> travellers)
    {
        var order = _harness.SeedOrder(offer, travellers);
        await ReserveAndConfirmAsync(order);
        return order;
    }

    private async Task ReserveAndConfirmAsync(Order order)
    {
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);
    }

    private OfferDetail Offer(IReadOnlyList<TravellerSpec> travellers)
        => OrderFixture.Offer(_harness.Clock, travellers, [new BoundSpec("OUT", 101)]);

    private static OfferDetail WithCoupons(OfferDetail offer, Func<OfferCoupon, OfferCoupon> change)
        => offer with
        {
            Tickets = offer.Tickets.Select(ticket => ticket with { Coupons = ticket.Coupons.Select(change).ToList() }).ToList()
        };

    private (int Tickets, int Tasks, int Saves, long NextNumber, int AirPriceCalls) Evidence()
        => (_harness.Tickets.Committed.Count,
            _harness.Tasks.Committed.Count,
            _harness.UnitOfWork.SaveCount,
            _harness.Stocks.Committed.Single().NextNumber,
            _harness.AirFareValidator.Calls.Count);

    private IEnumerable<TicketPriceLink> LinksOf(TicketCoupon coupon)
        => _harness.Tickets.Committed.Single(ticket => ticket.Id == coupon.TicketId).PriceLinks.Where(link => link.TicketCouponId == coupon.Id);

    private static OrderService ServiceOf(Order order, TicketCoupon coupon)
        => order.Services.Single(service => service.Id == coupon.OriginalOrderServiceId);

    private static long FlightOf(Order order, long segmentId) => order.Segments.Single(segment => segment.Id == segmentId).FlightId;

    private static IEnumerable<long> TargetIds(Domain.FulfillmentTaskAggregate.FulfillmentTask task, FulfillmentTargetKind kind)
        => task.Targets.Where(target => target.TargetKind == kind).Select(target => target.TargetId).Order();
}
