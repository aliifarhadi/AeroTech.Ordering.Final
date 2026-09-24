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

            builder.Property(interaction => interaction.Error).HasMaxLength(2000);

            builder.HasIndex(interaction => new { interaction.FulfillmentTaskId, interaction.AttemptNumber }).IsUnique();
        }
    }
}
