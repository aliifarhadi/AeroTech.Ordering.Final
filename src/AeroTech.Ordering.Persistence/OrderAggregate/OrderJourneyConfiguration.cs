using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderJourneyConfiguration : IEntityTypeConfiguration<OrderJourney>
    {
        public void Configure(EntityTypeBuilder<OrderJourney> builder)
        {
            builder.ToTable("OrderJourneys");
            builder.HasKey(journey => journey.Id);
            builder.Property(journey => journey.Id).ValueGeneratedNever();

            builder.Property(journey => journey.BoundId).HasMaxLength(64).IsRequired();

            builder.HasIndex(journey => new { journey.OrderId, journey.Sequence }).IsUnique();
        }
    }
}
