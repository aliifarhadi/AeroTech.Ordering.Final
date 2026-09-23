using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderSegmentReadModelConfiguration : IEntityTypeConfiguration<OrderSegmentReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderSegmentReadModel> builder)
        {
            builder.ToTable("OrderSegments");
            builder.HasKey(segment => segment.Id);
            builder.Property(segment => segment.Id).ValueGeneratedNever();

            builder.Property(segment => segment.FlightNumber).HasMaxLength(16);

            builder.HasIndex(segment => segment.OrderId);
            builder.HasIndex(segment => segment.OrderJourneyId);
            builder.HasIndex(segment => segment.SoldDeparture);
        }
    }
}
