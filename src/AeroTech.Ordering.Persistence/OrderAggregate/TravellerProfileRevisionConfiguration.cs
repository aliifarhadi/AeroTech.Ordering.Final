using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class TravellerProfileRevisionConfiguration : IEntityTypeConfiguration<TravellerProfileRevision>
    {
        public void Configure(EntityTypeBuilder<TravellerProfileRevision> builder)
        {
            builder.ToTable("TravellerProfileRevisions");
            builder.HasKey(revision => revision.Id);
            builder.Property(revision => revision.Id).ValueGeneratedNever();

            builder.Property(revision => revision.GivenName).HasMaxLength(64).IsRequired();
            builder.Property(revision => revision.Surname).HasMaxLength(64);

            builder.HasIndex(revision => revision.TravellerId);
            builder.HasIndex(revision => revision.CreatedByChangeId);
        }
    }
}
