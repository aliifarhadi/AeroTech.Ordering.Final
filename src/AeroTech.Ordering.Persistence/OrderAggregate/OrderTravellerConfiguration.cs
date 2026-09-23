using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderTravellerConfiguration : IEntityTypeConfiguration<OrderTraveller>
    {
        public void Configure(EntityTypeBuilder<OrderTraveller> builder)
        {
            builder.ToTable("OrderTravellers");
            builder.HasKey(traveller => traveller.Id);
            builder.Property(traveller => traveller.Id).ValueGeneratedNever();

            builder.Property(traveller => traveller.SourceTravellerRef).HasMaxLength(64);

            builder.HasMany(traveller => traveller.Documents)
                .WithOne()
                .HasForeignKey(document => document.OrderTravellerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(traveller => traveller.ProfileRevisions)
                .WithOne()
                .HasForeignKey(revision => revision.TravellerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(traveller => traveller.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(traveller => traveller.ProfileRevisions).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(traveller => new { traveller.OrderId, traveller.Index }).IsUnique();
            builder.HasIndex(traveller => traveller.InfantParentTravellerId);
        }
    }
}
