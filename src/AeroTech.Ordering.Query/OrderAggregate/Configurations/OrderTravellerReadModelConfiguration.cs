using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderTravellerReadModelConfiguration : IEntityTypeConfiguration<OrderTravellerReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderTravellerReadModel> builder)
        {
            builder.ToTable("OrderTravellers");
            builder.HasKey(traveller => traveller.Id);
            builder.Property(traveller => traveller.Id).ValueGeneratedNever();

            builder.Property(traveller => traveller.SourceTravellerRef).HasMaxLength(64);
            builder.Property(traveller => traveller.GivenName).HasMaxLength(64).IsRequired();
            builder.Property(traveller => traveller.Surname).HasMaxLength(64);

            builder.HasIndex(traveller => new { traveller.OrderId, traveller.Index }).IsUnique();
            builder.HasIndex(traveller => traveller.Surname);
        }
    }
}
