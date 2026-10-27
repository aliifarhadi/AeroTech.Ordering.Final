using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderSegmentLegReadModelConfiguration : IEntityTypeConfiguration<OrderSegmentLegReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderSegmentLegReadModel> builder)
        {
            builder.ToTable("OrderSegmentLegs");
            builder.HasKey(leg => leg.Id);
            builder.Property(leg => leg.Id).ValueGeneratedNever();

            builder.HasIndex(leg => leg.OrderId);
            builder.HasIndex(leg => leg.OrderSegmentId);
        }
    }
}
