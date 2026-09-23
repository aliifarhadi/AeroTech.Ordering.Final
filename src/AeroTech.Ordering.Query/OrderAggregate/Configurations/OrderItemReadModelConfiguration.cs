using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderItemReadModelConfiguration : IEntityTypeConfiguration<OrderItemReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderItemReadModel> builder)
        {
            builder.ToTable("OrderItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Id).ValueGeneratedNever();

            builder.HasIndex(item => new { item.OrderId, item.CommercialStatus });
        }
    }
}
