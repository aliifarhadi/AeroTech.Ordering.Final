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

            builder.Property(service => service.EquipmentKind).HasMaxLength(64);
            builder.Property(service => service.BagRef).HasMaxLength(128);

            builder.OwnsOne(service => service.Dimensions, dimensions =>
            {
                dimensions.Property(value => value.LengthCm).HasColumnName("LengthCm");
                dimensions.Property(value => value.WidthCm).HasColumnName("WidthCm");
                dimensions.Property(value => value.HeightCm).HasColumnName("HeightCm");
            });
        }
    }
}
