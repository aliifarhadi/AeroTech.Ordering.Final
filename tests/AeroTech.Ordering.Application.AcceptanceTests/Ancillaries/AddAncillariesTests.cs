using System.Globalization;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class AddAncillariesTests
{
    private readonly ReservationHarness _harness = new();

    public static TheoryData<string> GeneralVariantCodes => new(GeneralVariants);

    [Theory]
    [MemberData(nameof(GeneralVariantCodes))]
    public async Task Every_general_variant_is_accepted_as_one_item_and_one_service_in_one_add_product_change(string variantCode)
    {
        var order = await TwoTravellerOrderAsync();
        var line = LineFor(order, variantCode);

        var result = await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), line));

        var variant = AncillaryVariant.Find(variantCode)!;
        var change = order.Changes.Single(item => item.Id == result.OrderChangeId);
        var service = Assert.IsAssignableFrom<OrderAncillaryService>(order.Services.Single(item => item.CreatedByChangeId == change.Id));
        var item = order.Items.Single(candidate => candidate.Id == service.OrderItemId);

        Assert.Equal((OrderChangeType.AddProduct, 2, 2), (change.ChangeType, change.CommercialVersion, order.CommercialVersion));
        Assert.Equal((variant.ProductType, variant.ServiceType, variantCode, variant.Profile), (item.Kind, service.ServiceType, service.VariantCode, service.Profile));
        Assert.IsType(FamilyEntityOf(variant.Profile), service);
        Assert.Equal(line.Definition.DefinitionVersionId, service.Definition.DefinitionVersionId);
        Assert.Equal(line.Provision.ProvisionId, service.Provision.ProvisionId);
        Assert.Equal((line.Pricing.PricingId, line.Pricing.PricingRateId), (service.Pricing.PricingId, service.Pricing.PricingRateId));
        Assert.Equal(line.OfferItemId, service.Source.OfferItemId);
        Assert.Equal(10m, item.AcceptedTotal);
        Assert.Equal([new AddedAncillaryServiceShape(service.Id, variant.ServiceType, variantCode)], result.Services.Select(added => new AddedAncillaryServiceShape(added.OrderServiceId, added.ServiceType, added.VariantCode)));
        Assert.Equal(FulfillmentProviderKeys(), service.FulfillmentProviderKey);
    }

    [Fact]
    public async Task Included_tax_is_counted_once_and_every_component_is_kept_as_its_own_line()
    {
        var order = await TwoTravellerOrderAsync();
        var before = order.CustomerTotal;
        var traveller = Traveller(order, 1);
        var pricing = Price(
            100m,
            [Tax("YQ", 10m, TaxTreatment.IncludedInBase), Tax("IR", 5m, TaxTreatment.AddedToBase)],
            [Fee("SF", 3m, FeeApplicationUnit.Item)],
            unitAmount: 108m,
            quantity: 2);

        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.PaidPreorderMeal, traveller.Id, FlightRef("OUT", 1), pricing)));

        var lines = order.PricingLines.Where(line => line.Reason == OrderPricingReason.AdditionalSale).ToList();

        Assert.Equal(
            [(OrderPricingLineCategory.Ancillary, OrderPricingLineSubCategory.AncillaryBase, (string?)null, 180m), (OrderPricingLineCategory.Tax, OrderPricingLineSubCategory.Tax, "YQ", 20m), (OrderPricingLineCategory.Tax, OrderPricingLineSubCategory.Tax, "IR", 10m), (OrderPricingLineCategory.Fee, OrderPricingLineSubCategory.ServiceFee, "SF", 6m)],
            lines.Select(line => (line.Category, line.SubCategory, line.Code, line.Amount)));
        Assert.All(lines, line => Assert.Equal(OrderPricingLineDirection.Credit, line.Direction));
        Assert.Equal(before + 216m, order.CustomerTotal);
        Assert.Equal(TaxTreatment.IncludedInBase, lines.Single(line => line.Code == "YQ").SourceTaxTreatment);
    }

    public static TheoryData<string, int> RejectedPrices => new()
    {
        { "included-tax-double-count", 2851 },
        { "non-item-fee", 2850 },
        { "incomplete", 2847 },
        { "unknown-tax-treatment", 2849 },
        { "foreign-currency", 2848 }
    };

    [Theory]
    [MemberData(nameof(RejectedPrices))]
    public async Task A_price_the_source_does_not_prove_complete_is_refused_without_any_change(string scenario, int code)
    {
        var order = await TwoTravellerOrderAsync();
        var traveller = Traveller(order, 1);
        var pricing = scenario switch
        {
            "included-tax-double-count" => Price(100m, [Tax("YQ", 10m, TaxTreatment.IncludedInBase), Tax("IR", 5m, TaxTreatment.AddedToBase)], [], unitAmount: 115m),
            "non-item-fee" => Price(100m, [], [Fee("BF", 10m, FeeApplicationUnit.Ticket)], unitAmount: 110m),
            "incomplete" => Price(100m, [], [], unitAmount: 100m, isComplete: false),
            "unknown-tax-treatment" => Price(100m, [Tax("XX", 5m, TaxTreatment.LegacyUnknown)], [], unitAmount: 105m),
            _ => Price(100m, [], [], unitAmount: 100m, currencyId: 2)
        };

        await AssertRefusedWithoutChangeAsync(order, code, Sector(AncillaryVariant.PaidPreorderMeal, traveller.Id, FlightRef("OUT", 1), pricing));
    }

    [Fact]
    public async Task A_selection_priced_at_another_commercial_version_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))) with { OrderCommercialVersion = 7 };

        await AssertRefusedWithoutChangeAsync(order, 2829, selection);
    }

    [Fact]
    public async Task A_selection_priced_for_another_order_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))) with { SourceReference = "999" };

        await AssertRefusedWithoutChangeAsync(order, 2830, selection);
    }

    [Fact]
    public async Task An_expired_selection_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))) with { ExpiresAt = _harness.Clock.GetDateTime() };

        await AssertRefusedWithoutChangeAsync(order, 2828, selection);
    }

    [Fact]
    public async Task A_selection_that_lost_a_line_on_the_way_is_refused_as_a_whole()
    {
        var order = await TwoTravellerOrderAsync();
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))) with { SelectedLineCount = 2 };

        await AssertRefusedWithoutChangeAsync(order, 2831, selection);
    }

    [Fact]
    public async Task A_failing_line_leaves_no_line_of_the_basket_behind()
    {
        var order = await TwoTravellerOrderAsync();
        var traveller = Traveller(order, 1);
        var good = Sector(AncillaryVariant.FreeSpecialMeal, traveller.Id, FlightRef("OUT", 1));
        var bad = Sector(AncillaryVariant.PaidPreorderMeal, traveller.Id, FlightRef("OUT", 9));

        await AssertRefusedWithoutChangeAsync(order, 2843, Selection(order, _harness.Clock.GetDateTime(), good, bad));
    }

    [Fact]
    public async Task An_unexpected_commercial_version_is_refused_before_the_source_is_called()
    {
        var order = await TwoTravellerOrderAsync();
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1)));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.AddAncillariesAsync(order, selection, expectedCommercialVersion: 5));

        Assert.Equal((2825, 409), (refused.Code, refused.HttpStatus));
        Assert.Empty(_harness.Ancillaries.Requests);
    }

    [Fact]
    public async Task An_order_with_reservation_history_does_not_accept_ancillaries()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.ReserveOrderAsync(order);
        var selection = Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1)));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.AddAncillariesAsync(order, selection));

        Assert.Equal(order.Status == OrderStatus.Created ? 2826 : 2824, refused.Code);
        Assert.Empty(_harness.Ancillaries.Requests);
    }

    [Fact]
    public async Task A_portion_service_covers_every_flight_of_its_bound_with_one_price()
    {
        var order = await TwoTravellerOrderAsync();
        var traveller = Traveller(order, 1);

        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Portion(AncillaryVariant.ExtraCheckedBag, traveller.Id, "OUT")));

        var baggage = order.Services.OfType<OrderBaggageService>().Single();
        var expected = new[] { 101L, 102L }.Select(flightId => ReservationHarness.AirService(order, 1, flightId).Id);

        Assert.Equal(expected, baggage.CoveredAirServiceIds);
        Assert.Single(order.PricingLines, line => line.Allocations.Any(allocation => allocation.OrderServiceId == baggage.Id));
        Assert.Null(order.PricingLines.Single(line => line.Allocations.Any(allocation => allocation.OrderServiceId == baggage.Id)).Allocations.Single().OrderSegmentId);
    }

    [Fact]
    public async Task A_whole_order_service_has_no_traveller_and_no_flight_coverage()
    {
        var order = await TwoTravellerOrderAsync();

        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), WholeOrder(AncillaryVariant.OnboardWifi)));

        var service = order.Services.OfType<OrderAncillaryService>().Single();

        Assert.Null(service.TravellerId);
        Assert.Empty(service.CoveredAirServiceIds);
        Assert.Equal((SelectionScope.PerOrder, ServiceCoverageScope.Order), (service.SelectionScope, service.CoverageScope));
    }

    [Fact]
    public async Task Journey_coverage_without_its_flights_is_refused()
    {
        var order = await TwoTravellerOrderAsync();

        await AssertRefusedWithoutChangeAsync(order, 2845, Journey(AncillaryVariant.PriorityBoardingCheckin, Traveller(order, 1).Id, []));
    }

    [Fact]
    public async Task An_unknown_traveller_is_refused()
    {
        var order = await TwoTravellerOrderAsync();

        await AssertRefusedWithoutChangeAsync(order, 2842, Sector(AncillaryVariant.FreeSpecialMeal, 424242, FlightRef("OUT", 1)));
    }

    [Fact]
    public async Task A_required_choice_missing_from_the_source_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var line = Sector(AncillaryVariant.Overweight, Traveller(order, 1).Id, FlightRef("OUT", 1));
        var withoutBag = line with { Details = new ResolvedAncillaryDetails { Baggage = line.Details.Baggage! with { BagRef = null } } };

        await AssertRefusedWithoutChangeAsync(order, 2836, withoutBag);
    }

    [Fact]
    public async Task Priority_without_explicit_opt_in_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var line = Sector(AncillaryVariant.PriorityBoardingCheckin, Traveller(order, 1).Id, FlightRef("OUT", 1));
        var optedOut = line with { Details = new ResolvedAncillaryDetails { Priority = line.Details.Priority! with { OptIn = false } } };

        await AssertRefusedWithoutChangeAsync(order, 2837, optedOut);
    }

    [Fact]
    public async Task A_dynamic_upgrade_quote_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var line = Sector(AncillaryVariant.CabinUpgrade, Traveller(order, 1).Id, FlightRef("OUT", 1));
        var dynamic = line with { Details = new ResolvedAncillaryDetails { Upgrade = line.Details.Upgrade! with { AllowedUpgradeKind = UpgradeKind.DynamicQuote } } };

        await AssertRefusedWithoutChangeAsync(order, 2839, dynamic);
    }

    [Fact]
    public async Task An_order_holding_an_ancillary_cannot_be_reserved_and_no_provider_is_called()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.ReserveOrderAsync(order));

        Assert.Equal((2854, 409), (refused.Code, refused.HttpStatus));
        Assert.Empty(_harness.FlightFlow.HoldRequests);
        Assert.Empty(await _harness.Reservations.ListByOrderAsync(order.Id));
    }

    [Fact]
    public async Task Cancelling_the_whole_order_ends_its_ancillaries_too()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Portion(AncillaryVariant.ExtraCheckedBag, Traveller(order, 1).Id, "OUT"), WholeOrder(AncillaryVariant.OnboardWifi)));

        await _harness.CancelAsync(order);

        Assert.All(order.Services, service => Assert.Equal(OrderServiceCommercialState.Cancelled, service.CommercialStatus));
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task An_ancillary_cannot_be_cancelled_on_its_own()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("OUT", 1))));
        var meal = order.Services.OfType<OrderAncillaryService>().Single();

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, meal.Id));

        Assert.Equal(2856, refused.Code);
        Assert.True(meal.IsActive);
    }

    [Fact]
    public async Task Cancelling_the_flight_a_sector_service_covers_ends_the_service_with_it()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Sector(AncillaryVariant.FreeSpecialMeal, Traveller(order, 1).Id, FlightRef("RET", 1))));
        var meal = order.Services.OfType<OrderAncillaryService>().Single();

        await _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 201).Id);

        Assert.Equal(OrderServiceCommercialState.Cancelled, meal.CommercialStatus);
    }

    [Fact]
    public async Task Cancelling_one_flight_of_a_multi_flight_service_needs_a_requote()
    {
        var order = await TwoTravellerOrderAsync();
        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), Portion(AncillaryVariant.ExtraCheckedBag, Traveller(order, 1).Id, "OUT")));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order, ReservationHarness.AirService(order, 1, 102).Id));

        Assert.Equal((2855, 409), (refused.Code, refused.HttpStatus));
        Assert.All(order.Services, service => Assert.True(service.IsActive));
    }

    [Fact]
    public async Task Priced_seats_are_one_per_traveller_and_flight_with_their_own_item_and_point_of_sale()
    {
        var order = await TwoTravellerOrderAsync();
        var first = Traveller(order, 1);
        var second = Traveller(order, 2);
        var quote = SeatQuote(order, _harness.Clock.GetDateTime(), Seat(first.Id, 101, "12A"), Seat(first.Id, 102, "18C", AncillaryVariant.PreferredSeat, 40m), Seat(second.Id, 101, "12B"));

        order.AddAncillaries(null, quote, order.CommercialVersion, false, ReservationHarness.CancellingActor, _harness.Ids, _harness.Clock.GetDateTime());

        var seats = order.Services.OfType<OrderSeatService>().Where(seat => seat.IsPricedSelection).ToList();

        Assert.Equal(
            [(first.Id, 101L, "12A", AncillaryVariant.StandardSeat), (first.Id, 102L, "18C", AncillaryVariant.PreferredSeat), (second.Id, 101L, "12B", AncillaryVariant.StandardSeat)],
            seats.Select(seat => (seat.RequiredTravellerId, FlightOf(order, seat.SegmentId), seat.SeatNumber, seat.VariantCode!)));
        Assert.All(seats, seat => Assert.Equal(7, seat.PointOfSaleId));
        Assert.All(seats, seat => Assert.Equal(ProductType.Seat, order.Items.Single(item => item.Id == seat.OrderItemId).Kind));
        Assert.Equal(3, seats.Select(seat => seat.OrderItemId).Distinct().Count());
        Assert.Throws<BusinessException>(order.EnsureFulfillmentIsSupported);
    }

    [Fact]
    public async Task The_same_physical_seat_cannot_go_to_two_travellers()
    {
        var order = await TwoTravellerOrderAsync();
        var quote = SeatQuote(order, _harness.Clock.GetDateTime(), Seat(Traveller(order, 1).Id, 101, "12A"), Seat(Traveller(order, 2).Id, 101, "12A"));

        var refused = Assert.Throws<BusinessException>(() => order.AddAncillaries(null, quote, order.CommercialVersion, false, ReservationHarness.CancellingActor, _harness.Ids, _harness.Clock.GetDateTime()));

        Assert.Equal((2860, 409), (refused.Code, refused.HttpStatus));
    }

    [Fact]
    public async Task A_traveller_cannot_hold_two_seats_on_one_flight()
    {
        var order = await TwoTravellerOrderAsync();
        var traveller = Traveller(order, 1);
        var quote = SeatQuote(order, _harness.Clock.GetDateTime(), Seat(traveller.Id, 101, "12A"), Seat(traveller.Id, 101, "14A"));

        var refused = Assert.Throws<BusinessException>(() => order.AddAncillaries(null, quote, order.CommercialVersion, false, ReservationHarness.CancellingActor, _harness.Ids, _harness.Clock.GetDateTime()));

        Assert.Equal((2859, 409), (refused.Code, refused.HttpStatus));
    }

    [Fact]
    public async Task A_seat_quote_without_point_of_sale_is_refused()
    {
        var order = await TwoTravellerOrderAsync();
        var quote = SeatQuote(order, _harness.Clock.GetDateTime(), Seat(Traveller(order, 1).Id, 101, "12A") with { PointOfSaleId = 0 });

        var refused = Assert.Throws<BusinessException>(() => order.AddAncillaries(null, quote, order.CommercialVersion, false, ReservationHarness.CancellingActor, _harness.Ids, _harness.Clock.GetDateTime()));

        Assert.Equal(2858, refused.Code);
    }

    private async Task<Order> TwoTravellerOrderAsync()
        => await _harness.CreateFromOfferAsync(
            [TravellerSpec.Adult(1), TravellerSpec.Adult(2)],
            [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", 201)]);

    private async Task AssertRefusedWithoutChangeAsync(Order order, int code, params ResolvedAncillaryLine[] lines)
        => await AssertRefusedWithoutChangeAsync(order, code, Selection(order, _harness.Clock.GetDateTime(), lines));

    private async Task AssertRefusedWithoutChangeAsync(Order order, int code, ResolvedAncillarySelection selection)
    {
        var (version, total, items, services, lines) = (order.CommercialVersion, order.CustomerTotal, order.Items.Count, order.Services.Count, order.PricingLines.Count);

        var refused = await Assert.ThrowsAsync<BusinessException>(() => _harness.AddAncillariesAsync(order, selection));

        Assert.Equal(code, refused.Code);
        Assert.Equal((version, total, items, services, lines), (order.CommercialVersion, order.CustomerTotal, order.Items.Count, order.Services.Count, order.PricingLines.Count));
    }

    private static ResolvedAncillaryLine LineFor(Order order, string variantCode)
    {
        var adult = Traveller(order, 1);
        var line = Sector(variantCode, adult.Id, FlightRef("OUT", 1));

        return variantCode == AncillaryVariant.Bassinet
            ? line with { Details = new ResolvedAncillaryDetails { Assistance = line.Details.Assistance! with { InfantRef = Ref(Traveller(order, 2)), GuardianRef = Ref(adult) } } }
            : line;
    }

    private static OrderTraveller Traveller(Order order, int index) => order.Travellers.Single(traveller => traveller.Index == index);

    private static string Ref(OrderTraveller traveller) => traveller.Id.ToString(CultureInfo.InvariantCulture);

    private static long FlightOf(Order order, long segmentId) => order.Segments.Single(segment => segment.Id == segmentId).FlightId;

    private static string FulfillmentProviderKeys() => Domain.Providers.FulfillmentProviderKeys.Ancillary;

    private sealed record AddedAncillaryServiceShape(long OrderServiceId, OrderServiceType ServiceType, string VariantCode);
}
