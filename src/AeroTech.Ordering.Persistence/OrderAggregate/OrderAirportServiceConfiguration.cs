using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderAirportServiceConfiguration : IEntityTypeConfiguration<OrderAirportService>
    {
        public void Configure(EntityTypeBuilder<OrderAirportService> builder)
        {
            builder.ToTable("OrderAirportServices");

            builder.Property(service => service.TerminalRef).HasMaxLength(64);
        }
    }
}
