using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.DocumentStockAggregate
{
    public sealed class DocumentStockAllocationConfiguration : IEntityTypeConfiguration<DocumentStockAllocation>
    {
        public void Configure(EntityTypeBuilder<DocumentStockAllocation> builder)
        {
            builder.ToTable("DocumentStockAllocations");
            builder.HasKey(allocation => allocation.Id);
            builder.Property(allocation => allocation.Id).ValueGeneratedNever();

            builder.Property(allocation => allocation.DocumentRole).HasMaxLength(64).IsRequired();
            builder.Property(allocation => allocation.DocumentNumber).HasMaxLength(32).IsRequired();

            builder.HasIndex(allocation => allocation.DocumentNumber).IsUnique();
            builder.HasIndex(allocation => new { allocation.DocumentStockId, allocation.IssueFulfillmentTaskId, allocation.DocumentRole })
                .IsUnique()
                .HasFilter($"[State] <> {(int)StockNumberState.Retired}");
            builder.HasIndex(allocation => allocation.IssueFulfillmentTaskId);
        }
    }
}
