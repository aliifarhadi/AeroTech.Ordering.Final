using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderContactPointReadModelConfiguration : IEntityTypeConfiguration<OrderContactPointReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderContactPointReadModel> builder)
        {
            builder.ToTable("OrderContactPoints");
            builder.HasKey(point => point.Id);
            builder.Property(point => point.Id).ValueGeneratedNever();

            builder.Property(point => point.Value).HasMaxLength(256).IsRequired();
            builder.Property(point => point.CountryCode).HasMaxLength(8);

            builder.HasIndex(point => point.OrderId);
            builder.HasIndex(point => point.OrderContactId);
        }
    }
}
