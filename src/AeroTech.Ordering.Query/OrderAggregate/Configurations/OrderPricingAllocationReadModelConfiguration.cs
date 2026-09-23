using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderPricingAllocationReadModelConfiguration : IEntityTypeConfiguration<OrderPricingAllocationReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderPricingAllocationReadModel> builder)
        {
            builder.ToTable("OrderPricingAllocations");
            builder.HasKey(allocation => allocation.Id);
            builder.Property(allocation => allocation.Id).ValueGeneratedNever();

            builder.HasIndex(allocation => allocation.OrderId);
            builder.HasIndex(allocation => allocation.PricingLineId);
            builder.HasIndex(allocation => allocation.TravellerId);
        }
    }
}
