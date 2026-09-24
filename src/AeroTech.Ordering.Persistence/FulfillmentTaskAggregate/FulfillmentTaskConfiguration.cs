using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentTaskAggregate
{
    public sealed class FulfillmentTaskConfiguration : IEntityTypeConfiguration<FulfillmentTask>
    {
        public void Configure(EntityTypeBuilder<FulfillmentTask> builder)
        {
            builder.ToTable("FulfillmentTasks");
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Id).ValueGeneratedNever();

            builder.Property(task => task.FulfillmentProviderKey).HasMaxLength(64).IsRequired();
            builder.Property(task => task.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(task => task.CorrelationReference).HasMaxLength(100).IsRequired();
            builder.Property(task => task.LastError).HasMaxLength(2000);

            builder.Ignore(task => task.IsResumable);

            builder.HasMany(task => task.Targets)
                .WithOne()
                .HasForeignKey(target => target.FulfillmentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(task => task.Attempts)
                .WithOne()
                .HasForeignKey(attempt => attempt.FulfillmentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(task => task.Interactions)
                .WithOne()
                .HasForeignKey(interaction => interaction.FulfillmentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(task => task.Targets).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(task => task.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(task => task.Interactions).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(task => task.OrderId);
            builder.HasIndex(task => new { task.FulfillmentReservationId, task.TaskType });
        }
    }
}
