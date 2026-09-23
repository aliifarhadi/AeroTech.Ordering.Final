using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderReadModelConfiguration : IEntityTypeConfiguration<OrderReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderReadModel> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.Id).ValueGeneratedNever();

            builder.Property(order => order.SourceOfferId).HasMaxLength(512).IsRequired();

            builder.HasIndex(order => order.OrderReference).IsUnique();
            builder.HasIndex(order => order.CustomerId);
            builder.HasIndex(order => order.Status);
            builder.HasIndex(order => order.CreatedAt);
            builder.HasIndex(order => new { order.OfficeKind, order.OfficeId });
        }
    }
}
