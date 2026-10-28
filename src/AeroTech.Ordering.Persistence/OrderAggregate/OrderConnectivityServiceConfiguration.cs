using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderConnectivityServiceConfiguration : IEntityTypeConfiguration<OrderConnectivityService>
    {
        public void Configure(EntityTypeBuilder<OrderConnectivityService> builder)
        {
            builder.ToTable("OrderConnectivityServices");
        }
    }
}
