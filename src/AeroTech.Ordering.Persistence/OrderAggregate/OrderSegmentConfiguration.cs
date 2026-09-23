using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderSegmentConfiguration : IEntityTypeConfiguration<OrderSegment>
    {
        public void Configure(EntityTypeBuilder<OrderSegment> builder)
        {
            builder.ToTable("OrderSegments");
            builder.HasKey(segment => segment.Id);
            builder.Property(segment => segment.Id).ValueGeneratedNever();

            builder.Property(segment => segment.FlightNumber).HasMaxLength(16);

            builder.HasMany(segment => segment.Legs)
                .WithOne()
                .HasForeignKey(leg => leg.OrderSegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(segment => segment.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(segment => segment.OrderJourneyId);
            builder.HasIndex(segment => segment.FlightId);
        }
    }
}
