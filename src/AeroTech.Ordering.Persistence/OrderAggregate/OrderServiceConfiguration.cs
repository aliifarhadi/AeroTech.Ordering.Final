using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderServiceConfiguration : IEntityTypeConfiguration<OrderService>
    {
        public void Configure(EntityTypeBuilder<OrderService> builder)
        {
            builder.ToTable("OrderServices");
            builder.UseTptMappingStrategy();
            builder.HasKey(service => service.Id);
            builder.Property(service => service.Id).ValueGeneratedNever();

            builder.Property(service => service.FulfillmentProviderKey).HasMaxLength(64).IsRequired();

            builder.HasIndex(service => service.OrderItemId);
            builder.HasIndex(service => service.TravellerId);
            builder.HasIndex(service => new { service.OrderId, service.CommercialStatus });
        }
    }
}
