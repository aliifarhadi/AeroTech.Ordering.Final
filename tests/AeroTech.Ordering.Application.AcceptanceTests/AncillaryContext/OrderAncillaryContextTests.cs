using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Framework.Presentation.Json;
using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext.Service;
using AeroTech.Ordering.Synchronizer.ElectronicTicketAggregate;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.AncillaryContext;

[Collection(MigratedDatabaseCollection.Name)]
public sealed class OrderAncillaryContextTests : IAsyncLifetime
{
    private const long TechnicalStopFlightId = 201;
    private const int TechnicalStopAirportId = 99;

    private static readonly JsonSerializerOptions PlatformJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new LongToStringJsonConverter(), new NullableLongToStringJsonConverter() }
    };

    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public OrderAncillaryContextTests(MigratedDatabaseFixture database)
    {
        _database = database.For(_harness.Clock);
        _harness.OfficeTimeZones.TimeZones[ReservationHarness.IssuingOfficeId] = "Asia/Tehran";
    }

    public Task InitializeAsync() => _database.ClearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Unknown_order_is_rejected_as_order_not_found()
    {
        await using var queries = _database.NewQueryContext();
        var handler = new ServiceGetOrderAncillaryContextQueryHandler(new GetOrderAncillaryContextService(queries));

        var rejected = await Assert.ThrowsAsync<BusinessException>(
            () => handler.Handle(new ServiceGetOrderAncillaryContextQuery(404), CancellationToken.None));

        Assert.Equal((2500, 404), (rejected.Code, rejected.HttpStatus));
    }

    [Fact]
    public async Task Created_order_returns_every_context_fact_stored_in_the_order()
    {
        TravellerSpec[] travellers = [TravellerSpec.Adult(1), TravellerSpec.Infant(2, 1)];
        var order = await _harness.CreateFromOfferAsync(RichOffer(travellers), travellers);

        var context = await ContextAsync(order);

        var adult = order.Travellers.Single(traveller => traveller.Index == 1);
        var infant = order.Travellers.Single(traveller => traveller.Index == 2);
        Assert.Equal(
            (order.Id, order.CommercialVersion, false, (DateTimeOffset?)null, order.SourceOfferId, order.CurrencyId, order.CustomerId, order.SalesContext.Channel, order.SalesContext.TravelAgencyId),
            (context.OrderId, context.CommercialVersion, context.AcceptsChanges, context.TicketedAt, context.SourceOfferId, context.CurrencyId, context.CustomerId, context.Sales.Channel, context.Sales.TravelAgencyId));
        Assert.Equal(
            order.Travellers.OrderBy(traveller => traveller.Index).Select(traveller => (traveller.Id, traveller.PassengerType, ProfileOf(traveller).DateOfBirth, traveller.InfantParentTravellerId)),
            context.Travellers.Select(traveller => (traveller.TravellerId, traveller.PassengerTypeCode, traveller.DateOfBirth, traveller.InfantParentTravellerId)));
        Assert.Equal(adult.Id, context.Travellers.Single(traveller => traveller.TravellerId == infant.Id).InfantParentTravellerId);
        Assert.Equal(Itinerary(order), Itinerary(context));
        Assert.Equal(2, context.Journeys.SelectMany(journey => journey.Segments).Single(segment => segment.FlightId == TechnicalStopFlightId).Legs.Count);
        Assert.Equal(AirServicesOf(ReservationHarness.AirServices(order)), AirServicesOf(context));
        Assert.All(context.AirServices, service => Assert.Equal(AirFareType.Public, service.FareType));
        Assert.Empty(context.AncillaryServices);
    }

    [Fact]
    public async Task Cancelled_air_services_and_the_itinerary_only_they_covered_are_left_out()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);
        var outbound = ReservationHarness.AirService(order, 1, 101);
        var inbound = ReservationHarness.AirService(order, 1, 201);

        await _harness.CancelAsync(order, outbound.Id);
        var context = await ContextAsync(order);

        Assert.Equal(OrderServiceCommercialState.Cancelled, outbound.CommercialStatus);
        Assert.Equal([inbound.Id], context.AirServices.Select(service => service.OrderServiceId));
        Assert.Equal(["RET"], context.Journeys.Select(journey => journey.BoundId));
        Assert.Equal([inbound.SegmentId], context.Journeys.SelectMany(journey => journey.Segments).Select(segment => segment.SegmentId));
        Assert.False(context.AcceptsChanges);
    }

    [Fact]
    public async Task Ticketed_order_returns_its_first_issue_time_and_open_coupons()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();

        await _harness.IssueAsync(order, stock.DocumentStockId);
        var context = await ContextAsync(order);

        Assert.Equal(_harness.Tickets.Committed.Min(ticket => ticket.IssuedAt), context.TicketedAt);
        Assert.Equal([true, true], context.AirServices.Select(service => service.CouponOpenForUse));
        Assert.False(context.AcceptsChanges);
    }

    [Fact]
    public async Task Voided_ticket_leaves_no_issue_time_and_a_closed_coupon()
    {
        var order = await ConfirmedAsync([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var stock = await _harness.DefineStockAsync();
        await _harness.IssueAsync(order, stock.DocumentStockId);

        await _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, _harness.Tickets.Committed.Single());
        var context = await ContextAsync(order);

        Assert.Null(context.TicketedAt);
        Assert.False(Assert.Single(context.AirServices).CouponOpenForUse);
        Assert.False(context.AcceptsChanges);
    }

    [Fact]
    public async Task Platform_json_writes_ids_as_strings_and_enums_by_name()
    {
        TravellerSpec[] travellers = [TravellerSpec.Adult(1)];
        var order = await _harness.CreateFromOfferAsync(RichOffer(travellers), travellers);

        var json = JsonDocument.Parse(JsonSerializer.Serialize(await ContextAsync(order), PlatformJson)).RootElement;

        var airService = json.GetProperty("airServices")[0];
        Assert.Equal(
            ["orderId", "commercialVersion", "acceptsChanges", "ticketedAt", "sourceOfferId", "currencyId", "customerId", "sales", "travellers", "journeys", "airServices", "ancillaryServices"],
            json.EnumerateObject().Select(property => property.Name));
        Assert.Equal(order.Id.ToString(CultureInfo.InvariantCulture), json.GetProperty("orderId").GetString());
        Assert.Equal("BackOffice", json.GetProperty("sales").GetProperty("channel").GetString());
        Assert.Equal("ADT", json.GetProperty("travellers")[0].GetProperty("passengerTypeCode").GetString());
        Assert.Equal(JsonValueKind.String, airService.GetProperty("flightCapacityId").ValueKind);
        Assert.Equal("Public", airService.GetProperty("fareType").GetString());
        Assert.Equal("Kg", airService.GetProperty("checkedBaggageAllowance").GetProperty("unit").GetString());
        Assert.Equal(JsonValueKind.Null, airService.GetProperty("couponOpenForUse").ValueKind);
        Assert.Equal(0, json.GetProperty("ancillaryServices").GetArrayLength());
    }

    private async Task<Order> ConfirmedAsync(IReadOnlyList<TravellerSpec> travellers, IReadOnlyList<BoundSpec> bounds)
    {
        var order = await _harness.CreateFromOfferAsync(travellers, bounds);
        await _harness.ReserveOrderAsync(order);
        await _harness.ConfirmReservedCapacityAsync(order);

        return order;
    }

    private async Task<OrderAncillaryContextDto> ContextAsync(Order order)
    {
        await using (var writer = _database.NewQueryContext())
        {
            await new OrderQueryDbSynchronizer(writer).ProjectServicedAsync(_harness.Synchronizer.OrderProjections.Last());
            await writer.SaveChangesAsync();

            var tickets = new ElectronicTicketQueryDbSynchronizer(writer);
            await tickets.ProjectIssuedAsync(_harness.Synchronizer.TicketProjections);
            await writer.SaveChangesAsync();
            await tickets.ProjectVoidedAsync(_harness.Synchronizer.VoidProjections);
            await writer.SaveChangesAsync();
        }

        await using var reader = _database.NewQueryContext();

        return await new ServiceGetOrderAncillaryContextQueryHandler(new GetOrderAncillaryContextService(reader))
            .Handle(new ServiceGetOrderAncillaryContextQuery(order.Id), CancellationToken.None);
    }

    private OfferDetail RichOffer(IReadOnlyList<TravellerSpec> travellers)
    {
        var offer = OrderFixture.Offer(_harness.Clock, travellers, [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", TechnicalStopFlightId)]);

        return offer with
        {
            Bounds = offer.Bounds.Select(bound => bound with { Flights = bound.Flights.Select(WithLegsAndTerminals).ToList() }).ToList(),
            PricingUnits = offer.PricingUnits
                .Select(unit => unit with { FareComponents = unit.FareComponents.Select(component => component with { FareFamily = "Basic", FareType = "Public" }).ToList() })
                .ToList(),
            Tickets = offer.Tickets
                .Select(ticket => ticket with
                {
                    Coupons = ticket.Coupons
                        .Select(coupon => coupon with { CheckedBaggage = new OfferBaggage(1, 23m, "Kg"), CabinBaggage = new OfferBaggage(1, 7m, "Kg") })
                        .ToList()
                })
                .ToList()
        };
    }

    private static OfferFlight WithLegsAndTerminals(OfferFlight flight)
        => flight with
        {
            FlightVersion = 3,
            OriginAirportTerminalId = 1,
            DestinationAirportTerminalId = 2,
            Legs = flight.FlightId == TechnicalStopFlightId
                ?
                [
                    Leg(flight, 1, flight.OriginAirportId, TechnicalStopAirportId, flight.DepartureDateTime, flight.DepartureDateTime.AddMinutes(25)),
                    Leg(flight, 2, TechnicalStopAirportId, flight.DestinationAirportId, flight.DepartureDateTime.AddMinutes(40), flight.ArrivalDateTime)
                ]
                : [Leg(flight, 1, flight.OriginAirportId, flight.DestinationAirportId, flight.DepartureDateTime, flight.ArrivalDateTime)]
        };

    private static OfferFlightLeg Leg(OfferFlight flight, int sequence, long originAirportId, long destinationAirportId, DateTimeOffset departure, DateTimeOffset arrival)
        => new(sequence, flight.FlightId * 10 + sequence, originAirportId, null, destinationAirportId, null, departure, arrival, null);

    private static TravellerProfileRevision ProfileOf(OrderTraveller traveller)
        => traveller.ProfileRevisions.Single(revision => revision.Id == traveller.CurrentProfileRevisionId);

    private static IEnumerable<string> Itinerary(Order order)
        => order.Journeys
            .OrderBy(journey => journey.Sequence)
            .SelectMany(journey => order.Segments
                .Where(segment => segment.OrderJourneyId == journey.Id)
                .OrderBy(segment => segment.Sequence)
                .Select(segment => string.Join(
                    "|",
                    journey.BoundId,
                    journey.Sequence,
                    journey.OriginAirportId,
                    journey.DestinationAirportId,
                    segment.Id,
                    segment.Sequence,
                    segment.FlightId,
                    segment.FlightVersion,
                    segment.FlightNumber,
                    segment.OriginAirportId,
                    segment.OriginAirportTerminalId,
                    segment.DestinationAirportId,
                    segment.DestinationAirportTerminalId,
                    segment.OperatingAirlineId,
                    segment.MarketingAirlineId,
                    segment.SoldDeparture.ToString("O"),
                    segment.SoldArrival.ToString("O"),
                    segment.AircraftId,
                    string.Join(",", segment.Legs.OrderBy(leg => leg.Sequence).Select(leg => LegFact(leg.LegId, leg.Sequence, leg.OriginAirportId, leg.DestinationAirportId, leg.DepartureDateTime, leg.ArrivalDateTime))))));

    private static IEnumerable<string> Itinerary(OrderAncillaryContextDto context)
        => context.Journeys.SelectMany(journey => journey.Segments.Select(segment => string.Join(
            "|",
            journey.BoundId,
            journey.Sequence,
            journey.OriginAirportId,
            journey.DestinationAirportId,
            segment.SegmentId,
            segment.Sequence,
            segment.FlightId,
            segment.FlightVersion,
            segment.FlightNumber,
            segment.OriginAirportId,
            segment.OriginAirportTerminalId,
            segment.DestinationAirportId,
            segment.DestinationAirportTerminalId,
            segment.OperatingAirlineId,
            segment.MarketingAirlineId,
            segment.DepartureAt.ToString("O"),
            segment.ArrivalAt.ToString("O"),
            segment.AircraftId,
            string.Join(",", segment.Legs.Select(leg => LegFact(leg.LegId, leg.Sequence, leg.OriginAirportId, leg.DestinationAirportId, leg.DepartureAt, leg.ArrivalAt))))));

    private static string LegFact(long legId, int sequence, int originAirportId, int destinationAirportId, DateTimeOffset departure, DateTimeOffset arrival)
        => $"{legId}/{sequence}/{originAirportId}/{destinationAirportId}/{departure:O}/{arrival:O}";

    private static IEnumerable<string> AirServicesOf(IEnumerable<OrderAirTransportService> services)
        => services
            .OrderBy(service => service.TravellerId)
            .ThenBy(service => service.SegmentId)
            .Select(service => string.Join(
                "|",
                service.Id,
                service.TravellerId,
                service.SegmentId,
                service.FlightCapacityId,
                service.AirFareId,
                service.FareBasis,
                service.FareFamily,
                service.FareType,
                service.BookingClass,
                service.RbdId,
                service.CabinClassId,
                Baggage(service.CheckedBaggageAllowance),
                Baggage(service.CabinBaggageAllowance),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable,
                (bool?)null));

    private static IEnumerable<string> AirServicesOf(OrderAncillaryContextDto context)
        => context.AirServices.Select(service => string.Join(
            "|",
            service.OrderServiceId,
            service.TravellerId,
            service.SegmentId,
            service.FlightCapacityId,
            service.AirFareId,
            service.FareBasis,
            service.FareFamily,
            service.FareType,
            service.BookingClass,
            service.RbdId,
            service.CabinClassId,
            Baggage(service.CheckedBaggageAllowance),
            Baggage(service.CabinBaggageAllowance),
            service.IsRefundable,
            service.IsChangeable,
            service.IsUpgradable,
            service.CouponOpenForUse));

    private static string? Baggage(BaggageAllowance? allowance)
        => allowance is null ? null : Baggage(allowance.Pieces, allowance.Weight, allowance.Unit);

    private static string? Baggage(BaggageAllowanceDto? allowance)
        => allowance is null ? null : Baggage(allowance.Pieces, allowance.Weight, allowance.Unit);

    private static string Baggage(int pieces, decimal weight, WeightUnit unit)
        => $"{pieces}/{weight.ToString("0.##", CultureInfo.InvariantCulture)}/{unit}";
}
