using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderSeatServiceConfiguration : IEntityTypeConfiguration<OrderSeatService>
    {
        public void Configure(EntityTypeBuilder<OrderSeatService> builder)
        {
            builder.ToTable("OrderSeatServices");

            builder.Property(service => service.SeatNumber).HasMaxLength(16).IsRequired();

            builder.HasIndex(service => service.SegmentId);
            builder.HasIndex(service => service.AssociatedAirServiceId);
        }
    }
}
