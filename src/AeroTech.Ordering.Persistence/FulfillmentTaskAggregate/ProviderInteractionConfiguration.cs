using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentTaskAggregate
{
    public sealed class ProviderInteractionConfiguration : IEntityTypeConfiguration<ProviderInteraction>
    {
        public void Configure(EntityTypeBuilder<ProviderInteraction> builder)
        {
            builder.ToTable("ProviderInteractions");
            builder.HasKey(interaction => interaction.Id);
            builder.Property(interaction => interaction.Id).ValueGeneratedNever();

            builder.Property(interaction => interaction.FulfillmentProviderKey).HasMaxLength(64).IsRequired();
            builder.Property(interaction => interaction.IdempotencyKey).HasMaxLength(100);
            builder.Property(interaction => interaction.CorrelationReference).HasMaxLength(100);
            builder.Property(interaction => interaction.RequestPayload).HasColumnType("nvarchar(max)").IsRequired();
            builder.Property(interaction => interaction.RequestHash).HasMaxLength(64).IsRequired();
            builder.Property(interaction => interaction.ResponsePayload).HasColumnType("nvarchar(max)");
            builder.Property(interaction => interaction.ResponseHash).HasMaxLength(64);
            builder.Property(interaction => interaction.ProviderOperationRef).HasMaxLength(100);
            builder.Property(interaction => interaction.Error).HasMaxLength(2000);

            builder.Ignore(interaction => interaction.IsInFlight);

            builder.HasIndex(interaction => new { interaction.FulfillmentTaskAttemptId, interaction.Sequence }).IsUnique();
        }
    }
}
