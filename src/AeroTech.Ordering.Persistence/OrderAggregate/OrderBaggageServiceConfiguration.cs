using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderBaggageServiceConfiguration : IEntityTypeConfiguration<OrderBaggageService>
    {
        public void Configure(EntityTypeBuilder<OrderBaggageService> builder)
        {
            builder.ToTable("OrderBaggageServices");

            builder.OwnsOne(service => service.Baggage, baggage =>
            {
                baggage.Property(value => value.ChargeKind).HasColumnName("ChargeKind");
                baggage.Property(value => value.AllowanceConcept).HasColumnName("AllowanceConcept");
                baggage.Property(value => value.PackageWeightKg).HasColumnName("PackageWeightKg");
                baggage.Property(value => value.WeightFromExclusiveKg).HasColumnName("WeightFromExclusiveKg");
                baggage.Property(value => value.WeightToInclusiveKg).HasColumnName("WeightToInclusiveKg");
                baggage.Property(value => value.EquipmentKind).HasColumnName("EquipmentKind").HasMaxLength(64);
                baggage.Property(value => value.BagRef).HasColumnName("BagRef").HasMaxLength(128);
                baggage.Property(value => value.MeasuredWeightKg).HasColumnName("MeasuredWeightKg");
                baggage.Property(value => value.WeightKg).HasColumnName("WeightKg");

                baggage.OwnsOne(value => value.Dimensions, dimensions =>
                {
                    dimensions.Property(value => value.LengthCm).HasColumnName("LengthCm");
                    dimensions.Property(value => value.WidthCm).HasColumnName("WidthCm");
                    dimensions.Property(value => value.HeightCm).HasColumnName("HeightCm");
                });
            });

            builder.Navigation(service => service.Baggage).IsRequired();
        }
    }
}
