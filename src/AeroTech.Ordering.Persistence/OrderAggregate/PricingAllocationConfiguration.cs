using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class PricingAllocationConfiguration : IEntityTypeConfiguration<PricingAllocation>
    {
        public void Configure(EntityTypeBuilder<PricingAllocation> builder)
        {
            builder.ToTable("PricingAllocations");
            builder.HasKey(allocation => allocation.Id);
            builder.Property(allocation => allocation.Id).ValueGeneratedNever();

            builder.HasIndex(allocation => allocation.PricingLineId);
            builder.HasIndex(allocation => allocation.OrderItemId);
            builder.HasIndex(allocation => allocation.OrderServiceId);
            builder.HasIndex(allocation => allocation.TravellerId);
        }
    }
}
