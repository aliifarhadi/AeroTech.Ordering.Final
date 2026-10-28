using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.AncillaryFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

[Collection(MigratedDatabaseCollection.Name)]
public sealed class AncillaryPersistenceTests : IAsyncLifetime
{
    private readonly ReservationHarness _harness = new();
    private readonly TestDatabase _database;

    public AncillaryPersistenceTests(MigratedDatabaseFixture database) => _database = database.For(_harness.Clock);

    public Task InitializeAsync() => _database.ClearAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Every_persisted_variant_and_priced_seat_survives_a_command_round_trip_unchanged()
    {
        var order = await OrderWithEveryPersistedVariantAsync(_harness);

        await using (var context = _database.NewContext())
        {
            await new OrderRepository(context).AddAsync(order);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = (await new OrderRepository(context).GetAsync(order.Id))!;

            Assert.Equal(PersistedPurchases(order), PersistedPurchases(reloaded));
            Assert.Equal(PersistedPricing(order), PersistedPricing(reloaded));
            Assert.Equal((order.CommercialVersion, order.CustomerTotal), (reloaded.CommercialVersion, reloaded.CustomerTotal));
            Assert.Contains(reloaded.Services.OfType<OrderAncillaryService>(), service => service.TravellerId is null);
        }
    }

    [Fact]
    public async Task Every_persisted_variant_and_priced_seat_is_projected_to_the_read_model()
    {
        var order = await OrderWithEveryPersistedVariantAsync(_harness);
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
        var order = await OrderWithEveryPersistedVariantAsync(_harness);
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
}
