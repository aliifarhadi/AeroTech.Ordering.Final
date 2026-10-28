using System.Globalization;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class AncillaryPersistenceTests : IAsyncLifetime
{
    private static readonly IReadOnlyCollection<string> SourceGatedVariants =
    [
        AncillaryVariant.ExtraSeat,
        AncillaryVariant.MedicalEquipment,
        AncillaryVariant.UnaccompaniedMinor
    ];

    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public AncillaryPersistenceTests() => _database = new TestDatabase(_harness.Clock);

    public async Task InitializeAsync()
    {
        await using var commands = _database.NewContext();
        await commands.Database.MigrateAsync();

        await using var queries = _database.NewQueryContext();
        await queries.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Every_persisted_variant_and_priced_seat_survives_a_command_round_trip_unchanged()
    {
        var order = await OrderWithEveryPersistedVariantAsync();

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(Purchases(order), Purchases(reloaded));
            Assert.Equal(PricingOf(order), PricingOf(reloaded));
            Assert.Equal((order.CommercialVersion, order.CustomerTotal), (reloaded.CommercialVersion, reloaded.CustomerTotal));
            Assert.Contains(reloaded.Services.OfType<OrderAncillaryService>(), service => service.TravellerId is null);
        }
    }

    [Fact]
    public async Task Every_persisted_variant_and_priced_seat_is_projected_to_the_read_model()
    {
        var order = await OrderWithEveryPersistedVariantAsync();
        var snapshot = _harness.Synchronizer.OrderProjections.Last();

        await using (var writer = _database.NewQueryContext())
        {
            await new OrderQueryDbSynchronizer(writer).ProjectServicedAsync(snapshot);
            await writer.SaveChangesAsync();
        }

        await using var reader = _database.NewQueryContext();

        var projected = await reader.OrderAncillaryServices.Where(service => service.OrderId == order.Id).ToListAsync();

        Assert.Equal(
            snapshot.Services.Where(service => service.Ancillary is not null).Select(service => service.ServiceId).Order(),
            projected.Select(service => service.Id).Order());
        Assert.Equal(
            order.Services.OfType<OrderAncillaryService>().Select(service => (service.Id, service.VariantCode, service.Definition.ServiceDefinitionRef, service.Pricing.PricingRateId)).OrderBy(row => row.Id),
            projected.Where(row => row.SelectionScope is not null).Select(row => (row.Id, row.VariantCode, row.ServiceDefinitionRef, row.PricingRateId)).OrderBy(row => row.Id));
        Assert.Equal(
            snapshot.Services.Count(service => service.TravellerId is null),
            await reader.OrderServices.Where(service => service.OrderId == order.Id && service.TravellerId == null).CountAsync());
    }

    [Fact]
    public async Task The_ancillary_context_lists_every_active_accepted_service_for_repeat_shopping()
    {
        var order = await OrderWithEveryPersistedVariantAsync();
        var snapshot = _harness.Synchronizer.OrderProjections.Last();

        await using (var writer = _database.NewQueryContext())
        {
            await new OrderQueryDbSynchronizer(writer).ProjectServicedAsync(snapshot);
            await writer.SaveChangesAsync();
        }

        await using var reader = _database.NewQueryContext();
        var context = (await new GetOrderAncillaryContextService(reader).ExecuteAsync(order.Id))!;

        Assert.Equal(
            snapshot.Services.Where(service => service.Ancillary is not null).Select(service => (service.ServiceId, service.TravellerId, service.Ancillary!.VariantCode, service.Ancillary.Quantity, string.Join(",", service.Ancillary.CoveredAirServiceIds))),
            context.AncillaryServices.Select(service => (service.OrderServiceId, service.TravellerId, service.VariantCode, service.Quantity, string.Join(",", service.CoveredAirServiceIds))));
        Assert.Contains(context.AncillaryServices, service => service.TravellerId is null && service.Profile == AeroTech.Messages.Ancillary.Enums.AncillaryProfile.Connectivity);
        Assert.False(context.AcceptsChanges);
    }

    private async Task<Order> OrderWithEveryPersistedVariantAsync()
    {
        var order = await _harness.CreateFromOfferAsync(
            [TravellerSpec.Adult(1), TravellerSpec.Adult(2)],
            [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", 201)]);
        var adult = order.Travellers.Single(traveller => traveller.Index == 1);
        var other = order.Travellers.Single(traveller => traveller.Index == 2);

        var lines = GeneralVariants
            .Where(code => !SourceGatedVariants.Contains(code))
            .Select(code => code == AncillaryVariant.Bassinet
                ? WithBassinetRefs(Sector(code, adult.Id, FlightRef("OUT", 1)), other, adult)
                : Sector(code, adult.Id, FlightRef("OUT", 1)))
            .Append(Portion(AncillaryVariant.ExtraCheckedBag, other.Id, "OUT"))
            .Append(WholeOrder(AncillaryVariant.OnboardWifi))
            .ToArray();

        await _harness.AddAncillariesAsync(order, Selection(order, _harness.Clock.GetDateTime(), lines));

        order.AddAncillaries(
            null,
            SeatQuote(order, _harness.Clock.GetDateTime(), Seat(adult.Id, 101, "12A"), Seat(adult.Id, 102, "18C", AncillaryVariant.PreferredSeat, 40m)),
            order.CommercialVersion,
            false,
            ReservationHarness.CancellingActor,
            _harness.Ids,
            _harness.Clock.GetDateTime());

        return order;
    }

    private static ResolvedAncillaryLine WithBassinetRefs(ResolvedAncillaryLine line, OrderTraveller infant, OrderTraveller guardian)
        => line with
        {
            Details = new ResolvedAncillaryDetails
            {
                Assistance = line.Details.Assistance! with
                {
                    InfantRef = infant.Id.ToString(CultureInfo.InvariantCulture),
                    GuardianRef = guardian.Id.ToString(CultureInfo.InvariantCulture)
                }
            }
        };

    private static IEnumerable<object> Purchases(Order order)
        => order.Services
            .Where(service => service is OrderAncillaryService or OrderSeatService { IsPricedSelection: true })
            .OrderBy(service => service.Id)
            .Select<OrderService, object>(service => service switch
            {
                OrderBaggageService baggage => (Common(baggage), baggage.Baggage),
                OrderAncillaryService ancillary => (Common(ancillary), ancillary.Meal, ancillary.Pet, ancillary.Assistance, ancillary.AirportService, ancillary.Priority, ancillary.Connectivity, ancillary.Upgrade),
                OrderSeatService seat => (seat.Id, seat.OrderItemId, seat.TravellerId, seat.SegmentId, seat.AssociatedAirServiceId, seat.SeatNumber, seat.VariantCode, seat.PointOfSaleId, seat.Source, seat.Definition, seat.Provision, seat.Pricing),
                _ => throw new InvalidOperationException()
            })
            .ToList();

    private static object Common(OrderAncillaryService service)
        => (service.Id, service.OrderItemId, service.TravellerId, service.ServiceType, service.Profile, service.VariantCode, service.Quantity, service.SelectionScope, service.CoverageScope, string.Join(",", service.CoveredAirServiceIds), service.Source, service.Definition, service.Provision, service.Pricing, service.FulfillmentProviderKey);

    private static IEnumerable<object> PricingOf(Order order)
        => order.PricingLines
            .OrderBy(line => line.Id)
            .Select(line => (object)(line.Id, line.Reason, line.Category, line.SubCategory, line.Direction, line.Code, line.Amount, line.TaxJurisdiction, line.SourceTaxTreatment, line.SourceFeeApplicationUnit, string.Join(",", line.Allocations.Select(allocation => $"{allocation.OrderItemId}/{allocation.OrderServiceId}/{allocation.OrderSegmentId}/{allocation.TravellerId}/{allocation.Amount.ToString("0.####", CultureInfo.InvariantCulture)}"))))
            .ToList();
}
