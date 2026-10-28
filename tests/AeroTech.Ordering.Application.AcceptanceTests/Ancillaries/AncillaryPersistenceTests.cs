using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Persistence.OrderAggregate;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById;
using AeroTech.Ordering.Query._Shared.Enums;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;
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
    public async Task Every_accepted_variant_is_projected_to_the_read_model()
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

    [Fact]
    public async Task The_backoffice_order_detail_carries_every_accepted_ancillary_with_its_customer_choices()
    {
        var order = await OrderWithEveryPersistedVariantAsync(_harness);

        await using (var writer = _database.NewQueryContext())
        {
            await new OrderQueryDbSynchronizer(writer).ProjectServicedAsync(_harness.Synchronizer.OrderProjections.Last());
            await writer.SaveChangesAsync();
        }

        await using var reader = _database.NewQueryContext();
        var detail = (await new GetBackofficeOrderByIdService(new GetOrderByIdService(reader), new ReferenceCodeReader(reader)).ExecuteAsync(order.Id))!;
        var services = detail.Items.SelectMany(item => item.Services).ToList();

        Assert.Equal(
            order.Services.OfType<OrderAncillaryService>()
                .OrderBy(service => service.Id)
                .Select(service => (service.Id, service.VariantCode, service.Quantity, service.CoverageScope.ToString(), string.Join(",", service.CoveredAirServiceIds), Comparable(ChoicesOf(service)))),
            services.Where(service => service.Ancillary?.SelectionScope is not null)
                .OrderBy(service => service.Id)
                .Select(service => (service.Id, service.Ancillary!.VariantCode, service.Ancillary.Quantity, service.Ancillary.CoverageScope!.Name, string.Join(",", service.Ancillary.CoveredAirServiceIds), Comparable(Assert.Single(ChoicesOf(service.Ancillary))))));
        Assert.All(services.Where(service => service.Ancillary is null), service => Assert.Contains(service.ServiceType.Name, new[] { nameof(OrderServiceType.AirTransportation), nameof(OrderServiceType.SeatAssignment) }));
    }

    private static object ChoicesOf(OrderAncillaryService service)
        => service switch
        {
            OrderBaggageService baggage => new BackofficeAncillaryBaggageDto(
                EnumValueDto.Of(baggage.ChargeKind),
                EnumValueDto.OfNullable(baggage.AllowanceConcept),
                baggage.PackageWeightKg,
                baggage.WeightFromExclusiveKg,
                baggage.WeightToInclusiveKg,
                baggage.EquipmentKind,
                baggage.BagRef,
                baggage.MeasuredWeightKg,
                DimensionsOf(baggage.Dimensions),
                baggage.WeightKg),
            OrderMealService meal => new BackofficeAncillaryMealDto(EnumValueDto.Of(meal.MealKind), meal.MealCode, meal.MenuItemRef, meal.DietaryCode, meal.ExclusiveMealFamilyCode),
            OrderPetService pet => new BackofficeAncillaryPetDto(
                EnumValueDto.Of(pet.TransportMode),
                EnumValueDto.Of(pet.AnimalType),
                pet.OtherAnimalCode,
                pet.AnimalAgeWeeks,
                pet.CombinedWeightKg,
                DimensionsOf(pet.CarrierDimensions)!,
                pet.DocumentAcknowledgements.ToList(),
                pet.SizeBracket),
            OrderAssistedTravelService assistance => new BackofficeAncillaryAssistanceDto(EnumValueDto.Of(assistance.AssistanceKind), assistance.AssistanceSsrCode, assistance.InfantTravellerId, assistance.GuardianTravellerId),
            OrderAirportService airport => new BackofficeAncillaryAirportServiceDto(
                EnumValueDto.Of(airport.Kind),
                airport.AirportId,
                airport.TerminalRef,
                airport.FacilityId,
                EnumValueDto.Of(airport.Direction),
                airport.TimeWithOffset,
                airport.GuestCount),
            OrderPriorityService priority => new BackofficeAncillaryPriorityDto(EnumValueDto.Of(priority.Kind), priority.PriorityZoneCode, priority.PriorityGroupCode, priority.FareBenefitRef, priority.OptIn),
            OrderConnectivityService connectivity => new BackofficeAncillaryConnectivityDto(
                EnumValueDto.Of(connectivity.PlanKind),
                connectivity.DurationMinutes,
                connectivity.IncludedDataMb,
                connectivity.MaxDevices,
                EnumValueDto.Of(connectivity.DeliveryStage),
                connectivity.DeviceCount),
            OrderUpgradeService upgrade => new BackofficeAncillaryUpgradeDto(upgrade.FromCabinId, upgrade.ToCabinId, EnumValueDto.Of(upgrade.UpgradeKind), upgrade.RequiresTicketExchange),
            _ => throw new InvalidOperationException(service.GetType().Name)
        };

    private static IReadOnlyList<object> ChoicesOf(BackofficeOrderAncillaryDto ancillary)
        => new object?[] { ancillary.Baggage, ancillary.Meal, ancillary.Pet, ancillary.Assistance, ancillary.AirportService, ancillary.Priority, ancillary.Connectivity, ancillary.Upgrade }
            .OfType<object>()
            .ToList();

    private static object Comparable(object choices)
        => choices is BackofficeAncillaryPetDto pet
            ? (pet with { DocumentAcknowledgements = [] }, string.Join(",", pet.DocumentAcknowledgements))
            : choices;

    private static OrderAncillaryDimensionsDto? DimensionsOf(ServiceDimensions? dimensions)
        => dimensions is null ? null : new OrderAncillaryDimensionsDto(dimensions.LengthCm, dimensions.WidthCm, dimensions.HeightCm);
}
