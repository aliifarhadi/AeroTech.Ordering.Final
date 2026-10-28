using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderAncillaryServiceReadModelConfiguration : IEntityTypeConfiguration<OrderAncillaryServiceReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderAncillaryServiceReadModel> builder)
        {
            builder.ToTable("OrderAncillaryServices");
            builder.HasKey(service => service.Id);
            builder.Property(service => service.Id).ValueGeneratedNever();

            builder.Property(service => service.VariantCode).HasMaxLength(8).IsRequired();
            builder.Property(service => service.SourceReference).HasMaxLength(512).IsRequired();
            builder.Property(service => service.SourceSelectionId).HasMaxLength(1024).IsRequired();
            builder.Property(service => service.SourceOfferItemId).HasMaxLength(1024);
            builder.Property(service => service.ServiceDefinitionRef).HasMaxLength(128).IsRequired();
            builder.Property(service => service.ServiceTypeCode).HasMaxLength(8).IsRequired();
            builder.Property(service => service.ServiceSubCode).HasMaxLength(8).IsRequired();
            builder.Property(service => service.Rfic).HasMaxLength(8);
            builder.Property(service => service.Rfisc).HasMaxLength(8);
            builder.Property(service => service.SsrCode).HasMaxLength(8);
            builder.Property(service => service.ProvisionFulfillmentProviderKey).HasMaxLength(64);
            builder.Property(service => service.BaggageEquipmentKind).HasMaxLength(64);
            builder.Property(service => service.BaggageBagRef).HasMaxLength(128);
            builder.Property(service => service.MealCode).HasMaxLength(16);
            builder.Property(service => service.MealMenuItemRef).HasMaxLength(128);
            builder.Property(service => service.MealDietaryCode).HasMaxLength(16);
            builder.Property(service => service.MealExclusiveFamilyCode).HasMaxLength(64);
            builder.Property(service => service.PetOtherAnimalCode).HasMaxLength(64);
            builder.Property(service => service.PetSizeBracket).HasMaxLength(64);
            builder.Property(service => service.AssistanceSsrCode).HasMaxLength(16);
            builder.Property(service => service.AirportServiceTerminalRef).HasMaxLength(64);
            builder.Property(service => service.PriorityZoneCode).HasMaxLength(64);
            builder.Property(service => service.PriorityGroupCode).HasMaxLength(64);
            builder.Property(service => service.PriorityFareBenefitRef).HasMaxLength(128);

            builder.HasIndex(service => service.OrderId);
        }
    }
}
