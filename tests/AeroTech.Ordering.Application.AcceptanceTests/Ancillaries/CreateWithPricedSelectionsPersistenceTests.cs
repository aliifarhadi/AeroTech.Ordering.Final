using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

[Collection(MigratedDatabaseCollection.Name)]
public sealed class CreateWithPricedSelectionsPersistenceTests : IAsyncLifetime
{
    private static readonly IReadOnlyList<TravellerSpec> Travellers = [TravellerSpec.Adult(1), TravellerSpec.Adult(2)];
    private static readonly IReadOnlyList<BoundSpec> Itinerary = [new BoundSpec("OUT", 101, 102), new BoundSpec("RET", 201)];

    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public CreateWithPricedSelectionsPersistenceTests(MigratedDatabaseFixture database) => _database = database.For(_harness.Clock);

    public Task InitializeAsync() => _database.ClearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_create_with_every_persisted_variant_and_priced_seats_reloads_unchanged()
    {
        var order = await CreateMixedOrderAsync();

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(PersistedPurchases(order), PersistedPurchases(reloaded));
            Assert.Equal(PersistedSeatSources(order), PersistedSeatSources(reloaded));
            Assert.Equal(PersistedPricing(order), PersistedPricing(reloaded));
            Assert.Equal(
                (order.CommercialVersion, order.CustomerTotal, order.Items.Count, order.Changes.Count),
                (reloaded.CommercialVersion, reloaded.CustomerTotal, reloaded.Items.Count, reloaded.Changes.Count));
        }
    }

    [Fact]
    public async Task The_created_read_model_holds_every_selected_service_and_item_price()
    {
        var order = await CreateMixedOrderAsync();
        var snapshot = Assert.Single(_harness.Synchronizer.OrderProjections);

        await using (var writer = _database.NewQueryContext())
        {
            await new OrderQueryDbSynchronizer(writer).ProjectCreatedAsync(snapshot);
            await writer.SaveChangesAsync();
        }

        await using var reader = _database.NewQueryContext();

        var projected = await reader.OrderAncillaryServices.Where(service => service.OrderId == order.Id).ToListAsync();
        var items = await reader.OrderItems.Where(item => item.OrderId == order.Id).ToListAsync();
        var header = await reader.Orders.SingleAsync(row => row.Id == order.Id);

        Assert.Equal(
            order.Services.Where(service => service is OrderAncillaryService or OrderSeatService { IsPricedSelection: true }).Select(service => service.Id).Order(),
            projected.Select(service => service.Id).Order());
        Assert.Equal(
            order.Items.Select(item => (item.Id, item.AcceptedTotal)).OrderBy(item => item.Id),
            items.Select(item => (item.Id, item.AcceptedTotal)).OrderBy(item => item.Id));
        Assert.Equal((order.CustomerTotal, 1), (header.CustomerTotal, header.CommercialVersion));
        Assert.Contains(projected, service => service.SourceSelectionId == SeatSelectionId && service.SourceOfferItemId == Seat("T1", 101, "12A").SeatSelectionId);
        Assert.Contains(projected, service => service.SourceSelectionId == GeneralSelectionId && service.SourceReference == OfferId);
    }

    private Task<Order> CreateMixedOrderAsync()
        => _harness.CreateWithSelectionsAsync(
            OrderFixture.Offer(_harness.Clock, Travellers, Itinerary),
            Travellers,
            OfferSelection(_harness.Clock.GetDateTime(), EveryPersistedVariantAtCreate()),
            OfferSeatQuote(_harness.Clock.GetDateTime(), Seat("T1", 101, "12A"), Seat("T2", 201, "8C", AncillaryVariant.PreferredSeat, 40m)));
}
