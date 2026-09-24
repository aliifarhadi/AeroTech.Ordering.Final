using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.FareTopology;

public sealed class FareTopologyPersistenceTests : IAsyncLifetime
{
    private readonly FixedClock _clock = new();
    private readonly TestDatabase _database;

    public FareTopologyPersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Round_trip_topology_survives_persistence_and_reload()
    {
        var order = OrderFixture.Create(
            new SequentialIdGenerator(),
            _clock,
            [TravellerSpec.Adult(1), TravellerSpec.Adult(2)],
            [new BoundSpec("OUT", 101), new BoundSpec("IN", 201, 202)],
            pricingUnits:
            [
                new PricingUnitSpec(PricingUnitKind.RoundTripFare, ["OUT", "IN"], new FareSpec(9001, 101), new FareSpec(9002, 201, 202))
            ]);

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = await new OrderRepository(context).GetAsync(order.Id);

            Assert.NotNull(reloaded);
            Assert.Equal(Topology(order), Topology(reloaded));
        }
    }

    [Fact]
    public async Task Child_entities_are_stamped_with_the_time_they_were_saved()
    {
        var order = OrderFixture.Create(new SequentialIdGenerator(), _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101, 102)]);

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            IEnumerable<DateTimeOffset> stamps =
            [
                reloaded.LastUpdateTime,
                .. reloaded.Services.Select(service => service.LastUpdateTime),
                .. reloaded.Segments.Select(segment => segment.LastUpdateTime),
                .. reloaded.Journeys.Select(journey => journey.LastUpdateTime),
                .. reloaded.FarePricingUnits.Select(unit => unit.LastUpdateTime),
                .. reloaded.FarePricingUnits.SelectMany(unit => unit.FareComponents).Select(component => component.LastUpdateTime)
            ];

            Assert.All(stamps, stamp => Assert.Equal(_clock.Now, stamp));
        }
    }

    private static List<string> Topology(Order order)
        => order.FarePricingUnits
            .OrderBy(unit => unit.Sequence)
            .SelectMany(unit => unit.FareComponents
                .OrderBy(component => component.Sequence)
                .Select(component => string.Join(
                    "|",
                    unit.Id,
                    unit.OrderId,
                    unit.CreatedByChangeId,
                    unit.Sequence,
                    unit.Kind,
                    string.Join(",", unit.CoveredJourneyIds),
                    component.Id,
                    component.OrderFarePricingUnitId,
                    component.Sequence,
                    component.AirFareId,
                    component.BookingClass,
                    component.FareBasis,
                    component.FareFamily,
                    component.FareType,
                    string.Join(",", component.CoveredOrderServiceIds))))
            .ToList();
}
