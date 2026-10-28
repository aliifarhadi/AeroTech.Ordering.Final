using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderUpgradeServiceConfiguration : IEntityTypeConfiguration<OrderUpgradeService>
    {
        public void Configure(EntityTypeBuilder<OrderUpgradeService> builder)
        {
            builder.ToTable("OrderUpgradeServices");
        }
    }
}
