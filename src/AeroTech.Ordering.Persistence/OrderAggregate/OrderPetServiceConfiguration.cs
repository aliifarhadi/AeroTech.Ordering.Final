using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderPetServiceConfiguration : IEntityTypeConfiguration<OrderPetService>
    {
        public void Configure(EntityTypeBuilder<OrderPetService> builder)
        {
            builder.ToTable("OrderPetServices");

            builder.Property(service => service.OtherAnimalCode).HasMaxLength(64);
            builder.Property(service => service.SizeBracket).HasMaxLength(64);

            builder.OwnsOne(service => service.CarrierDimensions, dimensions =>
            {
                dimensions.Property(value => value.LengthCm).HasColumnName("CarrierLengthCm");
                dimensions.Property(value => value.WidthCm).HasColumnName("CarrierWidthCm");
                dimensions.Property(value => value.HeightCm).HasColumnName("CarrierHeightCm");
            });

            builder.Navigation(service => service.CarrierDimensions).IsRequired();

            builder.PrimitiveCollection(service => service.DocumentAcknowledgements)
                .HasField("_documentAcknowledgements")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();
        }
    }
}
