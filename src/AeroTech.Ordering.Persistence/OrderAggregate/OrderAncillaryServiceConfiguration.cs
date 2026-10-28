using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderAncillaryServiceConfiguration : IEntityTypeConfiguration<OrderAncillaryService>
    {
        public void Configure(EntityTypeBuilder<OrderAncillaryService> builder)
        {
            builder.ToTable("OrderAncillaryServices");

            builder.Property(service => service.VariantCode).HasMaxLength(8).IsRequired();

            builder.PrimitiveCollection(service => service.CoveredAirServiceIds)
                .HasField("_coveredAirServiceIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            builder.OwnsOne(service => service.Source, source => AncillarySaleTermsMapping.MapSource(source));
            builder.OwnsOne(service => service.Definition, definition => AncillarySaleTermsMapping.MapDefinition(definition));
            builder.OwnsOne(service => service.Provision, provision => AncillarySaleTermsMapping.MapProvision(provision));
            builder.OwnsOne(service => service.Pricing, pricing => AncillarySaleTermsMapping.MapPricing(pricing));

            builder.Navigation(service => service.Source).IsRequired();
            builder.Navigation(service => service.Definition).IsRequired();
            builder.Navigation(service => service.Provision).IsRequired();
            builder.Navigation(service => service.Pricing).IsRequired();

            builder.OwnsOne(service => service.Meal, meal =>
            {
                meal.Property(value => value.MealKind).HasColumnName("MealKind");
                meal.Property(value => value.MealCode).HasColumnName("MealCode").HasMaxLength(16);
                meal.Property(value => value.MenuItemRef).HasColumnName("MealMenuItemRef").HasMaxLength(128);
                meal.Property(value => value.DietaryCode).HasColumnName("MealDietaryCode").HasMaxLength(16);
                meal.Property(value => value.ExclusiveMealFamilyCode).HasColumnName("MealExclusiveFamilyCode").HasMaxLength(64);
            });

            builder.OwnsOne(service => service.Pet, pet =>
            {
                pet.Property(value => value.TransportMode).HasColumnName("PetTransportMode");
                pet.Property(value => value.AnimalType).HasColumnName("PetAnimalType");
                pet.Property(value => value.OtherAnimalCode).HasColumnName("PetOtherAnimalCode").HasMaxLength(64);
                pet.Property(value => value.AnimalAgeWeeks).HasColumnName("PetAnimalAgeWeeks");
                pet.Property(value => value.CombinedWeightKg).HasColumnName("PetCombinedWeightKg");
                pet.Property(value => value.SizeBracket).HasColumnName("PetSizeBracket").HasMaxLength(64);

                pet.OwnsOne(value => value.CarrierDimensions, dimensions =>
                {
                    dimensions.Property(value => value.LengthCm).HasColumnName("PetCarrierLengthCm");
                    dimensions.Property(value => value.WidthCm).HasColumnName("PetCarrierWidthCm");
                    dimensions.Property(value => value.HeightCm).HasColumnName("PetCarrierHeightCm");
                });

                pet.PrimitiveCollection(value => value.DocumentAcknowledgements)
                    .HasField("_documentAcknowledgements")
                    .UsePropertyAccessMode(PropertyAccessMode.Field)
                    .HasColumnName("PetDocumentAcknowledgements");
            });

            builder.OwnsOne(service => service.Assistance, assistance =>
            {
                assistance.Property(value => value.AssistanceKind).HasColumnName("AssistanceKind");
                assistance.Property(value => value.AssistanceSsrCode).HasColumnName("AssistanceSsrCode").HasMaxLength(16);
                assistance.Property(value => value.InfantTravellerId).HasColumnName("AssistanceInfantTravellerId");
                assistance.Property(value => value.GuardianTravellerId).HasColumnName("AssistanceGuardianTravellerId");
            });

            builder.OwnsOne(service => service.AirportService, airportService =>
            {
                airportService.Property(value => value.Kind).HasColumnName("AirportServiceKind");
                airportService.Property(value => value.AirportId).HasColumnName("AirportServiceAirportId");
                airportService.Property(value => value.TerminalRef).HasColumnName("AirportServiceTerminalRef").HasMaxLength(64);
                airportService.Property(value => value.FacilityId).HasColumnName("AirportServiceFacilityId");
                airportService.Property(value => value.Direction).HasColumnName("AirportServiceDirection");
                airportService.Property(value => value.TimeWithOffset).HasColumnName("AirportServiceTimeWithOffset");
                airportService.Property(value => value.GuestCount).HasColumnName("AirportServiceGuestCount");
            });

            builder.OwnsOne(service => service.Priority, priority =>
            {
                priority.Property(value => value.Kind).HasColumnName("PriorityKind");
                priority.Property(value => value.PriorityZoneCode).HasColumnName("PriorityZoneCode").HasMaxLength(64);
                priority.Property(value => value.PriorityGroupCode).HasColumnName("PriorityGroupCode").HasMaxLength(64);
                priority.Property(value => value.FareBenefitRef).HasColumnName("PriorityFareBenefitRef").HasMaxLength(128);
                priority.Property(value => value.OptIn).HasColumnName("PriorityOptIn");
            });

            builder.OwnsOne(service => service.Connectivity, connectivity =>
            {
                connectivity.Property(value => value.PlanKind).HasColumnName("ConnectivityPlanKind");
                connectivity.Property(value => value.DurationMinutes).HasColumnName("ConnectivityDurationMinutes");
                connectivity.Property(value => value.IncludedDataMb).HasColumnName("ConnectivityIncludedDataMb");
                connectivity.Property(value => value.MaxDevices).HasColumnName("ConnectivityMaxDevices");
                connectivity.Property(value => value.DeliveryStage).HasColumnName("ConnectivityDeliveryStage");
                connectivity.Property(value => value.DeviceCount).HasColumnName("ConnectivityDeviceCount");
            });

            builder.OwnsOne(service => service.Upgrade, upgrade =>
            {
                upgrade.Property(value => value.FromCabinId).HasColumnName("UpgradeFromCabinId");
                upgrade.Property(value => value.ToCabinId).HasColumnName("UpgradeToCabinId");
                upgrade.Property(value => value.UpgradeKind).HasColumnName("UpgradeKind");
                upgrade.Property(value => value.RequiresTicketExchange).HasColumnName("UpgradeRequiresTicketExchange");
            });

            builder.Ignore(service => service.MedicalEquipment);
            builder.Ignore(service => service.UnaccompaniedMinor);
            builder.Ignore(service => service.ExtraSeat);
        }
    }
}
