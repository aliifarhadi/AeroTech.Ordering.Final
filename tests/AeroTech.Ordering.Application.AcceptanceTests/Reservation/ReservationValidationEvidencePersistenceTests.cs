using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Persistence.FulfillmentReservationAggregate;
using AeroTech.Ordering.Persistence.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ReservationValidationEvidencePersistenceTests : IAsyncLifetime
{
    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public ReservationValidationEvidencePersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Evidence_survives_a_new_context_as_one_snapshot()
    {
        var order = TwoBoundOrder();
        var serviceIds = ReservationHarness.AirServices(order).Select(service => service.Id).ToList();
        var evidence = new ReservationValidationEvidence(order.CommercialVersion, _clock.Now.AddHours(2), _clock.Now, serviceIds);
        var reservation = await SaveAsync(order, evidence);

        var reloaded = await ReloadAsync(order);

        Assert.Equal(evidence, reloaded.ValidationEvidence);
        Assert.Equal(serviceIds, reloaded.ValidationEvidence!.ValidatedOrderServiceIds);
        Assert.Equal(evidence.ValidUntil, reloaded.ReservationValidationTimeLimit);
        Assert.Equal(reservation.Id, reloaded.Id);
    }

    [Fact]
    public async Task Legacy_timestamp_row_reloads_with_its_timestamp_and_without_canonical_evidence()
    {
        var order = TwoBoundOrder();
        var reservation = await SaveAsync(order, null);
        var legacyTimeLimit = _clock.Now.AddHours(2);

        await using (var context = _database.NewContext())
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Order].[FulfillmentReservations] SET [ReservationValidationTimeLimit] = {legacyTimeLimit} WHERE [Id] = {reservation.Id}");

        var reloaded = await ReloadAsync(order);

        Assert.Null(reloaded.ValidationEvidence);
        Assert.Equal(legacyTimeLimit, reloaded.ReservationValidationTimeLimit);
        Assert.False(reloaded.HasCurrentValidationFor(reloaded.CoveredOrderServiceIds, order.CommercialVersion, _clock.Now));
    }

    private Order TwoBoundOrder()
        => OrderFixture.Create(_ids, _clock, [TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101), new BoundSpec("RET", 201)]);

    private async Task<FulfillmentReservation> SaveAsync(Order order, ReservationValidationEvidence? evidence)
    {
        var reservation = FulfillmentReservation.Create(
            _ids.NewId(),
            order.Id,
            FulfillmentProviderKeys.FlightFlow,
            ReservationMode.HoldThenConfirm,
            _clock.Now.AddHours(1),
            evidence,
            ReservationHarness.AirServices(order).Select(service => new ReservationUnitIntent($"unit:{service.Id}", [service.Id], new UnitDetails())).ToList(),
            _ids,
            _clock.Now);

        await using var context = _database.NewContext();
        await new OrderRepository(context).AddAsync(order);
        await new FulfillmentReservationRepository(context).AddAsync(reservation);
        await context.SaveChangesAsync();

        return reservation;
    }

    private async Task<FulfillmentReservation> ReloadAsync(Order order)
    {
        await using var context = _database.NewContext();

        return (await new FulfillmentReservationRepository(context).ListByOrderAsync(order.Id)).Single();
    }

    private sealed record UnitDetails : ReservationUnitDetails;
}
