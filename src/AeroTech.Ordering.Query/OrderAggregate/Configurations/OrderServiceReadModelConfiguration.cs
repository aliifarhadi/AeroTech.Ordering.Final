using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderServiceReadModelConfiguration : IEntityTypeConfiguration<OrderServiceReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderServiceReadModel> builder)
        {
            builder.ToTable("OrderServices");
            builder.HasKey(service => service.Id);
            builder.Property(service => service.Id).ValueGeneratedNever();

            builder.Property(service => service.BookingClass).HasMaxLength(16);
            builder.Property(service => service.FareBasis).HasMaxLength(64);
            builder.Property(service => service.FareFamily).HasMaxLength(128);
            builder.Property(service => service.FareType).HasMaxLength(64);
            builder.Property(service => service.SeatNumber).HasMaxLength(16);

            builder.HasIndex(service => new { service.OrderId, service.CommercialStatus });
            builder.HasIndex(service => service.OrderItemId);
            builder.HasIndex(service => service.TravellerId);
            builder.HasIndex(service => service.SegmentId);
        }
    }
}
