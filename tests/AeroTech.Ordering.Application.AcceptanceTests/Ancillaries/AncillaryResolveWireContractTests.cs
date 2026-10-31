using System.Net;
using System.Text;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Providers.Ancillary.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class AncillaryResolveWireContractTests
{
    private const string SelectionId = "s_seats";

    private static readonly string Payload = ReadPayload("seat-resolve-offer.json");
    private static readonly string GeneralPayload = ReadPayload("general-resolve-offer.json");

    [Fact]
    public async Task The_adapter_posts_one_selection_id_and_keeps_the_one_selected_rate()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, Payload));

        var quote = await new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId);

        var (request, body) = Assert.Single(handler.Requests);
        var seat = Assert.Single(quote.Seats);

        Assert.Equal("/service/v1/Ancillaries/SeatMaps/Resolve", request.RequestUri!.AbsolutePath);
        Assert.Equal("""{"SelectionId":"s_seats"}""", body);
        Assert.Equal(
            (SelectionId, ShoppingSourceKind.Offer, "OFFER-1", (int?)null, 1),
            (quote.SelectionId, quote.SourceKind, quote.SourceReference, quote.OrderCommercialVersion, quote.SelectedLineCount));
        Assert.Equal(
            ("VDF8MTAxfDExfDEyQXw3", "A07", "T1", 101L, "12A", 7L),
            (seat.SeatSelectionId, seat.VariantCode, seat.TravellerRef, seat.FlightId, seat.SeatNumber, seat.PointOfSaleId));
        Assert.Equal(new ResolvedAncillaryDefinition(107, "SEAT_ECO_STANDARD", "A07", "F", "0B5", "A", "0B5", "SEAT"), seat.Definition);
        Assert.Equal(
            new ResolvedAncillaryProvision(207, ServiceCoverageScope.Sector, PurchaseStage.PreOrder, BookingMethod.Ssr, true, ConfirmationRequirement.Immediate, "FlightFlow", DocumentRouting.Emd, AncillaryDocumentType.EmdAssociated, true, true),
            seat.Provision);
        Assert.Equal(
            (11L, 12L, 3, 1, 1, 113m, 113m, PricingUnit.PerItem, 100m, true),
            (seat.Pricing.PricingId, seat.Pricing.PricingRateId, seat.Pricing.PricingVersion, seat.Pricing.CurrencyId, seat.Pricing.Quantity, seat.Pricing.UnitAmount, seat.Pricing.TotalAmount, seat.Pricing.PricingUnit, seat.Pricing.BaseAmount, seat.Pricing.IsUnitTotalComplete));
        Assert.Equal(
            ("VAT", 10m, TaxTreatment.AddedToBase, 364),
            (seat.Pricing.TaxLines.Single().Code, seat.Pricing.TaxLines.Single().Amount, seat.Pricing.TaxLines.Single().TaxTreatment!.Value, seat.Pricing.TaxLines.Single().CountryId!.Value));
        Assert.Equal(("SF", 3m, FeeApplicationUnit.Item), (seat.Pricing.FeeLines.Single().Code, seat.Pricing.FeeLines.Single().Amount, seat.Pricing.FeeLines.Single().FeeApplicationUnit!.Value));
    }

    [Fact]
    public async Task A_mapped_quote_is_accepted_by_create_with_its_taxes_and_fees()
    {
        var harness = new ReservationHarness();
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, Payload));
        var quote = await new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId);
        var travellers = new[] { TravellerSpec.Adult(1) };

        var order = await harness.CreateWithSelectionsAsync(OrderFixture.Offer(harness.Clock, travellers, [new BoundSpec("OUT", 101)]), travellers, seats: quote);

        var seat = Assert.Single(order.Services.OfType<OrderSeatService>());

        Assert.Equal(113m, order.Items.Single(item => item.Id == seat.OrderItemId).AcceptedTotal);
        Assert.Equal(100m + 113m, order.CustomerTotal);
    }

    [Theory]
    [InlineData("\"sourceKind\": \"Offer\",", "")]
    [InlineData("\"sourceReference\": \"OFFER-1\",", "")]
    [InlineData("\"selectedLineCount\": 1,", "")]
    [InlineData("\"selectedLineCount\": 1,\n    \"seats\"", "\"seats\"")]
    [InlineData("\"isUnitTotalComplete\": true,", "")]
    [InlineData("\"variantCode\": \"A07\",", "\"variantCode\": null,")]
    [InlineData("\"name\": \"Sector\"", "\"name\": \"Galaxy\"")]
    [InlineData("\"routing\": { \"value\": 2, \"name\": \"Emd\", \"title\": \"EMD\" },", "\"routing\": null,")]
    [InlineData("\"name\": \"AddedToBase\"", "\"name\": \"Unknown\"")]
    public async Task A_missing_or_unrecognised_source_fact_is_refused(string present, string replacement)
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, Normalised(Payload).Replace(present, replacement)));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId));

        Assert.Equal(2858, refused.Code);
    }

    [Fact]
    public async Task A_seat_row_without_its_expiry_is_refused()
    {
        var withoutRowExpiry = Normalised(Payload).Replace("\"pointOfSaleId\": \"7\",\n        \"expiresAtUtc\": \"2026-12-01T10:00:00+00:00\",", "\"pointOfSaleId\": \"7\",");
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, withoutRowExpiry));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId));

        Assert.Equal(2858, refused.Code);
    }

    [Fact]
    public async Task The_earliest_of_the_header_and_seat_expiries_governs_the_quote()
    {
        var earlierRow = Normalised(Payload).Replace("\"pointOfSaleId\": \"7\",\n        \"expiresAtUtc\": \"2026-12-01T10:00:00+00:00\",", "\"pointOfSaleId\": \"7\",\n        \"expiresAtUtc\": \"2026-12-01T09:00:00+00:00\",");
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, earlierRow));

        var quote = await new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId);

        Assert.Equal(new DateTimeOffset(2026, 12, 1, 9, 0, 0, TimeSpan.Zero), quote.ExpiresAt);
    }

    [Theory]
    [InlineData(HttpStatusCode.Gone, 16817, 2865)]
    [InlineData(HttpStatusCode.Conflict, 16820, 2863)]
    [InlineData(HttpStatusCode.NotFound, 16819, 2864)]
    [InlineData(HttpStatusCode.NotFound, 16811, 2863)]
    [InlineData(HttpStatusCode.BadGateway, 16813, 2857)]
    public async Task Source_failures_map_to_typed_refusals(HttpStatusCode status, int sourceCode, int code)
    {
        var handler = new StubHttpMessageHandler(_ => Json(status, $$"""{"errors":[{"code":{{sourceCode}},"title":"SOURCE_FAILED","detail":null,"metadata":null}]}"""));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSeatSelectionAsync(SelectionId));

        Assert.Equal(code, refused.Code);
    }

    [Fact]
    public async Task A_general_answer_reaches_the_order_with_its_quantity_journey_coverage_and_options()
    {
        var harness = new ReservationHarness();
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, GeneralPayload));
        var selection = await new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSelectionAsync("s_general");
        var travellers = new[] { TravellerSpec.Adult(1) };

        var order = await harness.CreateWithSelectionsAsync(
            OrderFixture.Offer(harness.Clock, travellers, [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]),
            travellers,
            selection);

        var bag = Assert.Single(order.Services.OfType<OrderBaggageService>());
        var priority = Assert.Single(order.Services.OfType<OrderPriorityService>());
        var airServices = order.Services.OfType<OrderAirTransportService>().Select(service => service.Id);

        Assert.Equal((ShoppingSourceKind.Offer, "OFFER-1", (int?)null, 2), (selection.SourceKind, selection.SourceReference, selection.OrderCommercialVersion, selection.SelectedLineCount));
        Assert.Equal((2, "d_bag"), (bag.Quantity, bag.Source.OfferItemId));
        Assert.True(priority.OptIn);
        Assert.Equal(airServices, priority.CoveredAirServiceIds);
        Assert.Equal(200m + 60m + 15m, order.CustomerTotal);
    }

    [Fact]
    public async Task The_general_resolve_answer_without_its_source_header_is_refused()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, """{"data":{"orderId":null,"orderCommercialVersion":null,"baggages":[],"meals":[],"pets":[],"assistedTravel":[],"airportServices":[],"priorityServices":[],"connectivity":[],"upgrades":[],"extraSeats":[],"travelInsurance":[]},"errors":null}"""));

        var refused = await Assert.ThrowsAsync<BusinessException>(() => new AncillaryProvider(StubHttpMessageHandler.ClientFor(handler)).ResolveSelectionAsync("s_general"));

        Assert.Equal(2858, refused.Code);
    }

    private static string ReadPayload(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ancillaries", "Payloads", fileName));

    private static string Normalised(string payload) => payload.Replace("\r\n", "\n");

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
