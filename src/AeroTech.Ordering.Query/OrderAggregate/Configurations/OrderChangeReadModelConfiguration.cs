using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderChangeReadModelConfiguration : IEntityTypeConfiguration<OrderChangeReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderChangeReadModel> builder)
        {
            builder.ToTable("OrderChanges");
            builder.HasKey(change => change.Id);
            builder.Property(change => change.Id).ValueGeneratedNever();

            builder.Property(change => change.SourceReference).HasMaxLength(512);
            builder.Property(change => change.SourceSystem).HasMaxLength(64);
            builder.Property(change => change.ReasonCode).HasMaxLength(64);
            builder.Property(change => change.WaiverCode).HasMaxLength(64);

            builder.HasIndex(change => new { change.OrderId, change.CommercialVersion });
        }
    }
}
