using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderTravellerDocumentConfiguration : IEntityTypeConfiguration<OrderTravellerDocument>
    {
        public void Configure(EntityTypeBuilder<OrderTravellerDocument> builder)
        {
            builder.ToTable("OrderTravellerDocuments");
            builder.HasKey(document => document.Id);
            builder.Property(document => document.Id).ValueGeneratedNever();

            builder.Property(document => document.Number).HasMaxLength(64).IsRequired();
            builder.Property(document => document.Holder).HasMaxLength(128).IsRequired();

            builder.HasIndex(document => document.OrderTravellerId);
        }
    }
}
