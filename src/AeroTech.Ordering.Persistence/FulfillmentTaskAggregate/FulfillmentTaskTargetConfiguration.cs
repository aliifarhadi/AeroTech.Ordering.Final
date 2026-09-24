using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentTaskAggregate
{
    public sealed class FulfillmentTaskTargetConfiguration : IEntityTypeConfiguration<FulfillmentTaskTarget>
    {
        public void Configure(EntityTypeBuilder<FulfillmentTaskTarget> builder)
        {
            builder.ToTable("FulfillmentTaskTargets");
            builder.HasKey(target => target.Id);
            builder.Property(target => target.Id).ValueGeneratedNever();

            builder.HasIndex(target => target.FulfillmentTaskId);
            builder.HasIndex(target => target.ReservationUnitId);
        }
    }
}
