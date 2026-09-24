using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentTaskAggregate
{
    public sealed class FulfillmentTaskAttemptConfiguration : IEntityTypeConfiguration<FulfillmentTaskAttempt>
    {
        public void Configure(EntityTypeBuilder<FulfillmentTaskAttempt> builder)
        {
            builder.ToTable("FulfillmentTaskAttempts");
            builder.HasKey(attempt => attempt.Id);
            builder.Property(attempt => attempt.Id).ValueGeneratedNever();

            builder.Property(attempt => attempt.Error).HasMaxLength(2000);

            builder.Ignore(attempt => attempt.IsInProgress);

            builder.HasIndex(attempt => new { attempt.FulfillmentTaskId, attempt.AttemptNumber }).IsUnique();
        }
    }
}
