using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderPricingLineReadModelConfiguration : IEntityTypeConfiguration<OrderPricingLineReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderPricingLineReadModel> builder)
        {
            builder.ToTable("OrderPricingLines");
            builder.HasKey(line => line.Id);
            builder.Property(line => line.Id).ValueGeneratedNever();

            builder.Property(line => line.Code).HasMaxLength(32);
            builder.Property(line => line.Description).HasMaxLength(256);
            builder.Property(line => line.Reference).HasMaxLength(64);

            builder.HasIndex(line => new { line.OrderId, line.Treatment });
        }
    }
}
