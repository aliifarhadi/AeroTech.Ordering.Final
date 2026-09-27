using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Providers.Offer.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class CabinClassPreservationTests : IAsyncLifetime
{
    private readonly FixedClock _clock = new();
    private readonly TestDatabase _database;

    public CabinClassPreservationTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public void Sold_cabin_class_is_preserved_from_the_offer_on_every_air_service()
    {
        var offer = OfferResponseMapper.ToDomain(CapturedOffers.WithBoundIdentity());

        var order = CreateFrom(offer);

        var cabinClassByFlight = offer.Bounds.SelectMany(bound => bound.Flights).ToDictionary(flight => flight.FlightId, flight => flight.CabinClassId);
        Assert.All(cabinClassByFlight.Values, cabinClassId => Assert.NotNull(cabinClassId));
        Assert.All(ReservationHarness.AirServices(order), service =>
            Assert.Equal(cabinClassByFlight[order.Segments.Single(segment => segment.Id == service.SegmentId).FlightId], service.CabinClassId));
    }

    [Fact]
    public void Missing_cabin_class_is_not_inferred_from_the_booking_class()
    {
        var source = OfferResponseMapper.ToDomain(CapturedOffers.WithBoundIdentity());
        var offer = source with
        {
            Bounds = source.Bounds
                .Select(bound => bound with { Flights = bound.Flights.Select(flight => flight with { CabinClassId = null }).ToList() })
                .ToList()
        };

        var order = CreateFrom(offer);

        Assert.All(ReservationHarness.AirServices(order), service =>
        {
            Assert.NotNull(service.RbdId);
            Assert.Null(service.CabinClassId);
        });
    }

    [Fact]
    public async Task Sold_cabin_class_survives_persistence_and_reload()
    {
        var order = CreateFrom(OfferResponseMapper.ToDomain(CapturedOffers.WithBoundIdentity()));

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(CabinClasses(order), CabinClasses(reloaded));
        }
    }

    private Order CreateFrom(OfferDetail offer)
        => OrderFixture.CreateFrom(offer, new SequentialIdGenerator(), _clock, [TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);

    private static IReadOnlyList<(long ServiceId, long? CabinClassId)> CabinClasses(Order order)
        => ReservationHarness.AirServices(order).Select(service => (service.Id, service.CabinClassId)).OrderBy(item => item.Id).ToList();
}
