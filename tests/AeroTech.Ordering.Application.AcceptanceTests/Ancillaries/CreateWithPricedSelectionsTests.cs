using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.DomainEvents;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class CreateWithPricedSelectionsTests
{
    private const decimal AirFareTotal = 600m;

    private static readonly IReadOnlyList<TravellerSpec> Travellers = [TravellerSpec.Adult(1), TravellerSpec.Adult(2)];
    private static readonly IReadOnlyList<BoundSpec> Itinerary = [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", 201)];

    private readonly ReservationHarness _harness = new();

    public static TheoryData<string> GeneralVariantCodes => new(GeneralVariants);

    public static TheoryData<string, Func<ResolvedAncillarySelection, ResolvedAncillarySelection>, int> RefusedSelections => new()
    {
        { "another offer", selection => selection with { SourceReference = "OFFER-2" }, 2830 },
        { "order source", selection => selection with { SourceKind = ShoppingSourceKind.Order, SourceReference = "1", OrderCommercialVersion = 1 }, 2830 },
        { "offer with an order version", selection => selection with { OrderCommercialVersion = 1 }, 2830 },
        { "expired", selection => selection with { ExpiresAt = selection.ExpiresAt.AddMinutes(-30) }, 2828 },
        { "no lines", selection => selection with { Lines = [], SelectedLineCount = 0 }, 2832 },
        { "dropped line", selection => selection with { SelectedLineCount = selection.Lines.Count + 1 }, 2831 },
        { "repeated item", selection => selection with { Lines = [selection.Lines[0], selection.Lines[0]], SelectedLineCount = 2 }, 2833 },
        { "unknown traveller", selection => WithLine(selection, Sector(AncillaryVariant.ExtraCheckedBag, "T9", FlightRef("OUT", 1))), 2842 },
        { "unknown flight", selection => WithLine(selection, Sector(AncillaryVariant.ExtraCheckedBag, "T1", FlightRef("OUT", 9))), 2843 },
        { "unknown portion", selection => WithLine(selection, Portion(AncillaryVariant.ExtraCheckedBag, "T1", "XXX")), 2844 },
        { "unknown variant", selection => WithLine(selection, selection.Lines[0] with { VariantCode = "A99" }), 2834 },
        { "quantity mismatch", selection => WithLine(selection, selection.Lines[0] with { SelectedQuantity = 3 }), 2840 },
        { "incomplete price", selection => WithPrice(selection, Price(0m, [], [], 0m, isComplete: false)), 2847 },
        { "another currency", selection => WithPrice(selection, Price(10m, [], [], 10m, currencyId: 2)), 2848 },
        { "unreconciled price", selection => WithPrice(selection, Price(10m, [], [], 9m)), 2851 },
        { "negative price", selection => WithPrice(selection, Price(-10m, [], [], -10m)), 2852 }
    };

    public static TheoryData<string, Func<ResolvedSeatQuote, ResolvedSeatQuote>, int> RefusedSeatQuotes => new()
    {
        { "another offer", quote => quote with { SourceReference = "OFFER-2" }, 2830 },
        { "order source", quote => quote with { SourceKind = ShoppingSourceKind.Order, SourceReference = "1", OrderCommercialVersion = 1 }, 2830 },
        { "expired", quote => quote with { ExpiresAt = quote.ExpiresAt.AddMinutes(-30) }, 2828 },
        { "no seats", quote => quote with { Seats = [], SelectedLineCount = 0 }, 2832 },
        { "dropped seat", quote => quote with { SelectedLineCount = quote.Seats.Count + 1 }, 2831 },
        { "repeated seat id", quote => quote with { Seats = [quote.Seats[0], quote.Seats[0]], SelectedLineCount = 2 }, 2833 },
        { "second seat for a traveller", quote => WithSeats(quote, Seat("T1", 101, "12A"), Seat("T1", 101, "14A")), 2859 },
        { "one seat for two travellers", quote => WithSeats(quote, Seat("T1", 101, "12A"), Seat("T2", 101, "12A")), 2860 },
        { "unknown traveller", quote => WithSeats(quote, Seat("T9", 101, "12A")), 2842 },
        { "flight outside the offer", quote => WithSeats(quote, Seat("T1", 999, "12A")), 2843 },
        { "extra seat on the seat map", quote => WithSeats(quote, Seat("T1", 101, "12A", AncillaryVariant.ExtraSeat)), 2835 },
        { "missing point of sale", quote => WithSeats(quote, Seat("T1", 101, "12A") with { PointOfSaleId = 0 }), 2858 },
        { "incomplete rate", quote => WithSeats(quote, Seat("T1", 101, "12A") with { Pricing = Price(0m, [], [], 0m, isComplete: false) }), 2847 },
        { "another currency", quote => WithSeats(quote, Seat("T1", 101, "12A") with { Pricing = Price(10m, [], [], 10m, currencyId: 2) }), 2848 }
    };

    private DateTimeOffset Now => _harness.Clock.GetDateTime();

    [Fact]
    public async Task An_air_only_create_calls_no_ancillary_source()
    {
        var order = await CreateAsync();

        Assert.Empty(_harness.Ancillaries.Requests);
        AssertOneCreateChange(order);
        Assert.Equal(AirFareTotal, order.CustomerTotal);
        Assert.All(order.Items, item => Assert.Equal(ProductType.AirFare, item.Kind));
    }

    [Fact]
    public async Task Legacy_seat_selections_keep_their_bound_wide_behaviour()
    {
        var order = await CreateAsync(legacySeats: [new SeatSpec(1, "OUT", "12A")]);

        var seats = order.Services.OfType<OrderSeatService>().ToList();

        Assert.Equal(2, seats.Count);
        Assert.All(seats, seat => Assert.Equal(("12A", false), (seat.SeatNumber, seat.IsPricedSelection)));
        Assert.DoesNotContain(order.Items, item => item.Kind == ProductType.Seat);
        Assert.Empty(_harness.Ancillaries.Requests);
    }

    [Fact]
    public async Task General_lines_of_different_families_join_the_initial_order_in_one_create_change()
    {
        var baggage = Sector(AncillaryVariant.ExtraCheckedBag, "T1", FlightRef("OUT", 1), Price(30m, quantity: 2));
        var meal = Portion(AncillaryVariant.PaidPreorderMeal, "T2", "RET");

        var order = await CreateAsync(OfferSelection(Now, baggage, meal));

        var bag = Assert.Single(order.Services.OfType<OrderBaggageService>());
        var menu = Assert.Single(order.Services.OfType<OrderMealService>());

        AssertOneCreateChange(order);
        Assert.Equal(2, bag.Quantity);
        Assert.Equal("MENU_7", menu.MenuItemRef);
        Assert.Equal(new AncillarySaleSource(ShoppingSourceKind.Offer, OfferId, null, GeneralSelectionId, baggage.OfferItemId), bag.Source);
        Assert.Equal((60m, 10m), (ItemOf(order, bag).AcceptedTotal, ItemOf(order, menu).AcceptedTotal));
        Assert.Equal(AirFareTotal + 70m, order.CustomerTotal);
    }

    [Fact]
    public async Task Priced_seats_become_standalone_seat_items_with_their_exact_source()
    {
        var first = Seat("T1", 101, "12A");
        var second = Seat("T2", 201, "8C", AncillaryVariant.PreferredSeat, 40m);

        var order = await CreateAsync(seats: OfferSeatQuote(Now, first, second));

        var seats = order.Services.OfType<OrderSeatService>().OrderBy(seat => seat.Id).ToList();

        AssertOneCreateChange(order);
        Assert.Equal(
            [(1, 101L, "12A", AncillaryVariant.StandardSeat, 25m), (2, 201L, "8C", AncillaryVariant.PreferredSeat, 40m)],
            seats.Select(seat => (Traveller(order, seat).Index, FlightOf(order, seat.SegmentId), seat.SeatNumber, seat.VariantCode!, ItemOf(order, seat).AcceptedTotal)));
        Assert.Equal(new AncillarySaleSource(ShoppingSourceKind.Offer, OfferId, null, SeatSelectionId, first.SeatSelectionId), seats[0].Source);
        Assert.All(seats, seat => Assert.Equal((7L, ProductType.Seat), (seat.PointOfSaleId!.Value, ItemOf(order, seat).Kind)));
        Assert.Equal(AirFareTotal + 65m, order.CustomerTotal);
    }

    [Fact]
    public async Task Air_general_lines_and_seats_are_priced_in_one_create_with_each_tax_counted_once()
    {
        var pricing = Price(
            100m,
            [Tax("YQ", 10m, TaxTreatment.IncludedInBase), Tax("IR", 5m, TaxTreatment.AddedToBase)],
            [Fee("SF", 3m, FeeApplicationUnit.Item)],
            unitAmount: 108m,
            quantity: 2);

        var order = await CreateAsync(
            OfferSelection(Now, Sector(AncillaryVariant.PaidPreorderMeal, "T1", FlightRef("OUT", 1), pricing), WholeOrder(AncillaryVariant.OnboardWifi)),
            OfferSeatQuote(Now, Seat("T1", 101, "12A"), Seat("T2", 101, "12B", price: 40m)));

        AssertOneCreateChange(order);
        Assert.Equal(AirFareTotal + 216m + 10m + 25m + 40m, order.CustomerTotal);
        Assert.All(order.Items, item => Assert.Equal(AllocatedTo(order, item), item.AcceptedTotal));
        Assert.Equal(order.CustomerTotal, order.GetEvents().OfType<OrderCreated>().Single().CustomerTotal);
        Assert.Equal(1, _harness.UnitOfWork.SaveCount);

        var projected = Assert.Single(_harness.Synchronizer.OrderProjections);

        Assert.Equal((order.CustomerTotal, 4), (projected.CustomerTotal, projected.Services.Count(service => service.Ancillary is not null)));
    }

    [Fact]
    public async Task A_complete_zero_rate_seat_is_accepted_with_no_price_line()
    {
        var order = await CreateAsync(seats: OfferSeatQuote(Now, Seat("T1", 101, "12A", price: 0m)));

        var seat = Assert.Single(order.Services.OfType<OrderSeatService>());

        Assert.Equal(0m, ItemOf(order, seat).AcceptedTotal);
        Assert.Equal(AirFareTotal, order.CustomerTotal);
        Assert.DoesNotContain(order.PricingLines, line => line.Allocations.Any(allocation => allocation.OrderServiceId == seat.Id));
    }

    [Theory]
    [MemberData(nameof(RefusedSelections))]
    public async Task A_refused_general_selection_leaves_no_order(string reason, Func<ResolvedAncillarySelection, ResolvedAncillarySelection> spoil, int code)
    {
        var selection = spoil(OfferSelection(Now, Sector(AncillaryVariant.ExtraCheckedBag, "T1", FlightRef("OUT", 1))));

        await AssertRefusedWithoutOrderAsync(reason, code, selection, null);
    }

    [Theory]
    [MemberData(nameof(RefusedSeatQuotes))]
    public async Task A_refused_seat_quote_leaves_no_order(string reason, Func<ResolvedSeatQuote, ResolvedSeatQuote> spoil, int code)
    {
        var quote = spoil(OfferSeatQuote(Now, Seat("T1", 101, "12A")));

        await AssertRefusedWithoutOrderAsync(reason, code, null, quote);
    }

    [Fact]
    public async Task A_valid_general_selection_is_not_kept_when_the_seats_belong_to_another_offer()
    {
        var selection = OfferSelection(Now, Sector(AncillaryVariant.ExtraCheckedBag, "T1", FlightRef("OUT", 1)));
        var seats = OfferSeatQuote(Now, Seat("T1", 101, "12A")) with { SourceReference = "OFFER-2" };

        await AssertRefusedWithoutOrderAsync("mixed offers", 2830, selection, seats);
    }

    [Fact]
    public async Task A_selection_unknown_to_the_source_leaves_no_order()
    {
        _harness.OfferProvider.Offers[OfferId] = OrderFixture.Offer(_harness.Clock, Travellers, Itinerary);

        var args = OrderFixture.Args(Travellers, []) with { AncillaryPricedSelectionId = "s_unknown" };

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.CreateAsync(args));

        Assert.Equal(2864, refused.Code);
        AssertNothingPersisted();
    }

    [Fact]
    public async Task A_portion_selection_is_one_service_covering_both_of_its_flights()
    {
        var order = await CreateAsync(OfferSelection(Now, Portion(AncillaryVariant.ExtraCheckedBag, "T1", "OUT")));

        var bag = Assert.Single(order.Services.OfType<OrderBaggageService>());

        Assert.Equal(AirServiceIdsOf(order, 1, 101, 102), bag.CoveredAirServiceIds);
        Assert.Equal(10m, ItemOf(order, bag).AcceptedTotal);
    }

    [Fact]
    public async Task An_offer_journey_selection_covers_every_flight_of_its_traveller_once()
    {
        var order = await CreateAsync(OfferSelection(Now, Journey(AncillaryVariant.PriorityBoardingCheckin, "T2", [])));

        var priority = Assert.Single(order.Services.OfType<OrderPriorityService>());

        Assert.Equal(2, Traveller(order, priority).Index);
        Assert.Equal(AirServiceIdsOf(order, 2, 101, 102, 201), priority.CoveredAirServiceIds);
        Assert.Equal(10m, ItemOf(order, priority).AcceptedTotal);
        Assert.Equal(AirFareTotal + 10m, order.CustomerTotal);
    }

    [Theory]
    [InlineData("named flights")]
    [InlineData("portion on the unit")]
    [InlineData("flight on the unit")]
    public async Task An_offer_journey_selection_that_contradicts_the_whole_itinerary_is_refused(string contradiction)
    {
        var journey = Journey(AncillaryVariant.PriorityBoardingCheckin, "T1", []);

        var line = contradiction switch
        {
            "named flights" => Journey(AncillaryVariant.PriorityBoardingCheckin, "T1", [FlightRef("OUT", 1)]),
            "portion on the unit" => journey with { Unit = journey.Unit with { PortionRef = "OUT" } },
            _ => journey with { Unit = journey.Unit with { FlightRef = FlightRef("OUT", 1) } }
        };

        await AssertRefusedWithoutOrderAsync(contradiction, 2846, OfferSelection(Now, line), null);
    }

    [Theory]
    [InlineData(AncillaryVariant.CipMeetAssist)]
    [InlineData(AncillaryVariant.OnboardWifi)]
    public async Task A_whole_order_selection_has_no_beneficiary_and_one_charge(string variantCode)
    {
        var order = await CreateAsync(OfferSelection(Now, WholeOrder(variantCode)));

        var service = Assert.Single(order.Services.OfType<OrderAncillaryService>());

        Assert.Null(service.TravellerId);
        Assert.Empty(service.CoveredAirServiceIds);
        Assert.Equal(AirFareTotal + 10m, order.CustomerTotal);
    }

    [Theory]
    [MemberData(nameof(GeneralVariantCodes))]
    public async Task Every_general_variant_is_accepted_at_create_as_its_own_family(string variantCode)
    {
        var line = Sector(variantCode, "T1", FlightRef("OUT", 1));

        var order = await CreateAsync(OfferSelection(Now, variantCode == AncillaryVariant.Bassinet ? WithBassinetRefs(line, "T2", "T1") : line));

        var variant = AncillaryVariant.Find(variantCode)!;
        var service = Assert.Single(order.Services.OfType<OrderAncillaryService>());

        Assert.IsType(FamilyEntityOf(variant.Profile), service);
        Assert.Equal((variant.ProductType, variant.ServiceType, variantCode, line.OfferItemId), (ItemOf(order, service).Kind, service.ServiceType, service.VariantCode, service.Source.OfferItemId));
        Assert.Equal(order.Changes.Single().Id, service.CreatedByChangeId);
        AssertOneCreateChange(order);
    }

    [Theory]
    [InlineData(AncillaryVariant.StandardSeat)]
    [InlineData(AncillaryVariant.PreferredSeat)]
    public async Task Every_seat_assignment_variant_is_accepted_at_create_as_a_priced_seat(string variantCode)
    {
        var order = await CreateAsync(seats: OfferSeatQuote(Now, Seat("T1", 101, "12A", variantCode)));

        var seat = Assert.Single(order.Services.OfType<OrderSeatService>());

        Assert.True(seat.IsPricedSelection);
        Assert.Equal((variantCode, OrderServiceType.SeatAssignment, ProductType.Seat), (seat.VariantCode, seat.ServiceType, ItemOf(order, seat).Kind));
    }

    [Fact]
    public async Task Legacy_seats_and_a_priced_seat_selection_are_refused_before_any_source_is_called()
    {
        var args = OrderFixture.Args(Travellers, [new SeatSpec(1, "OUT", "12A")]) with { SeatPricedSelectionId = SeatSelectionId };

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.CreateAsync(args));

        Assert.Equal((2866, 422), (refused.Code, refused.HttpStatus));
        Assert.Empty(_harness.OfferProvider.Requests);
        Assert.Empty(_harness.Ancillaries.Requests);
        AssertNothingPersisted();
    }

    [Fact]
    public void The_domain_refuses_legacy_seats_with_a_priced_seat_selection_on_its_own()
    {
        var args = OrderFixture.Args(Travellers, [new SeatSpec(1, "OUT", "12A")]) with { SeatPricedSelectionId = SeatSelectionId };

        var refused = Assert.Throws<BusinessException>(() => Order.Create(args, OrderFixture.Offer(_harness.Clock, Travellers, Itinerary), _harness.Ids, _harness.Clock));

        Assert.Equal(2866, refused.Code);
    }

    [Fact]
    public async Task Selections_accepted_at_create_keep_reservation_blocked_until_fulfillment_exists()
    {
        var order = await CreateAsync(seats: OfferSeatQuote(Now, Seat("T1", 101, "12A", price: 0m)));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal((2854, 409), (refused.Code, refused.HttpStatus));
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    private static ResolvedAncillarySelection WithLine(ResolvedAncillarySelection selection, ResolvedAncillaryLine line)
        => selection with { Lines = [line] };

    private static ResolvedAncillarySelection WithPrice(ResolvedAncillarySelection selection, ResolvedAncillaryPricing pricing)
        => WithLine(selection, Sector(AncillaryVariant.ExtraCheckedBag, "T1", FlightRef("OUT", 1), pricing));

    private static ResolvedSeatQuote WithSeats(ResolvedSeatQuote quote, params ResolvedSeat[] seats)
        => quote with { Seats = seats, SelectedLineCount = seats.Length };

    private static OrderItem ItemOf(Order order, OrderService service)
        => order.Items.Single(item => item.Id == service.OrderItemId);

    private static OrderTraveller Traveller(Order order, OrderService service)
        => order.Travellers.Single(traveller => traveller.Id == service.TravellerId);

    private static long FlightOf(Order order, long segmentId)
        => order.Segments.Single(segment => segment.Id == segmentId).FlightId;

    private static IReadOnlyList<long> AirServiceIdsOf(Order order, int travellerIndex, params long[] flightIds)
    {
        var traveller = order.Travellers.Single(item => item.Index == travellerIndex);

        return flightIds
            .Select(flightId => order.Services.OfType<OrderAirTransportService>()
                .Single(service => service.TravellerId == traveller.Id && FlightOf(order, service.SegmentId) == flightId).Id)
            .ToList();
    }

    private static decimal AllocatedTo(Order order, OrderItem item)
        => order.PricingLines
            .Where(line => line.Treatment == PricingLineTreatment.CustomerPrice)
            .SelectMany(line => line.Allocations)
            .Where(allocation => allocation.OrderItemId == item.Id)
            .Sum(allocation => allocation.Amount);

    private static void AssertOneCreateChange(Order order)
    {
        var change = Assert.Single(order.Changes);

        Assert.Equal((OrderChangeType.Create, 1, 1), (change.ChangeType, change.CommercialVersion, order.CommercialVersion));
        Assert.All(order.PricingLines, line => Assert.Equal((OrderPricingReason.InitialSale, change.Id), (line.Reason, line.CreatedByChangeId)));
        Assert.All(order.Services, service => Assert.Equal(change.Id, service.CreatedByChangeId));
    }

    private Task<Order> CreateAsync(
        ResolvedAncillarySelection? selection = null,
        ResolvedSeatQuote? seats = null,
        IReadOnlyList<SeatSpec>? legacySeats = null)
        => _harness.CreateWithSelectionsAsync(OrderFixture.Offer(_harness.Clock, Travellers, Itinerary), Travellers, selection, seats, legacySeats);

    private async Task AssertRefusedWithoutOrderAsync(string reason, int code, ResolvedAncillarySelection? selection, ResolvedSeatQuote? seats)
    {
        var refused = await Assert.ThrowsAsync<BusinessException>(() => CreateAsync(selection, seats));

        Assert.True(refused.Code == code, $"{reason}: expected {code}, got {refused.Code}");
        AssertNothingPersisted();
    }

    private void AssertNothingPersisted()
    {
        Assert.Equal(0, _harness.UnitOfWork.SaveCount);
        Assert.Empty(_harness.Synchronizer.OrderProjections);
    }
}
