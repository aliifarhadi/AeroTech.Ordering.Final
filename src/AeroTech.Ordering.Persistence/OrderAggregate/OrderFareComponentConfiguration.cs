using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderFareComponentConfiguration : IEntityTypeConfiguration<OrderFareComponent>
    {
        public void Configure(EntityTypeBuilder<OrderFareComponent> builder)
        {
            builder.ToTable("OrderFareComponents");
            builder.HasKey(component => component.Id);
            builder.Property(component => component.Id).ValueGeneratedNever();

            builder.Property(component => component.BookingClass).HasMaxLength(16);
            builder.Property(component => component.FareBasis).HasMaxLength(64);
            builder.Property(component => component.FareFamily).HasMaxLength(128);
            builder.Property(component => component.FareType).HasMaxLength(64);

            builder.PrimitiveCollection(component => component.CoveredOrderServiceIds)
                .HasField("_coveredOrderServiceIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            builder.HasIndex(component => new { component.OrderFarePricingUnitId, component.Sequence }).IsUnique();
        }
    }
}
