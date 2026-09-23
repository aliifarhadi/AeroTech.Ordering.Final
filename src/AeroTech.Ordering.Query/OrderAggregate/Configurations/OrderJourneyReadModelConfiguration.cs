using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderJourneyReadModelConfiguration : IEntityTypeConfiguration<OrderJourneyReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderJourneyReadModel> builder)
        {
            builder.ToTable("OrderJourneys");
            builder.HasKey(journey => journey.Id);
            builder.Property(journey => journey.Id).ValueGeneratedNever();

            builder.Property(journey => journey.BoundId).HasMaxLength(64).IsRequired();

            builder.HasIndex(journey => new { journey.OrderId, journey.Sequence }).IsUnique();
        }
    }
}
