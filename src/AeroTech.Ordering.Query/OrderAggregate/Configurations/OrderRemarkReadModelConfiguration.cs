using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderRemarkReadModelConfiguration : IEntityTypeConfiguration<OrderRemarkReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderRemarkReadModel> builder)
        {
            builder.ToTable("OrderRemarks");
            builder.HasKey(remark => remark.Id);
            builder.Property(remark => remark.Id).ValueGeneratedNever();

            builder.Property(remark => remark.Text).HasMaxLength(512).IsRequired();
            builder.Property(remark => remark.CategoryCode).HasMaxLength(32);

            builder.HasIndex(remark => new { remark.OrderId, remark.Status });
            builder.HasIndex(remark => remark.SupersedesRemarkId);
        }
    }
}
