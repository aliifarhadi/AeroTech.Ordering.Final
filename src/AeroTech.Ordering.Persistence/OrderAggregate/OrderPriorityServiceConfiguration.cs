using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderPriorityServiceConfiguration : IEntityTypeConfiguration<OrderPriorityService>
    {
        public void Configure(EntityTypeBuilder<OrderPriorityService> builder)
        {
            builder.ToTable("OrderPriorityServices");

            builder.Property(service => service.PriorityZoneCode).HasMaxLength(64);
            builder.Property(service => service.PriorityGroupCode).HasMaxLength(64);
            builder.Property(service => service.FareBenefitRef).HasMaxLength(128);
        }
    }
}
