using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderTravellerDocumentReadModelConfiguration : IEntityTypeConfiguration<OrderTravellerDocumentReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderTravellerDocumentReadModel> builder)
        {
            builder.ToTable("OrderTravellerDocuments");
            builder.HasKey(document => document.Id);
            builder.Property(document => document.Id).ValueGeneratedNever();

            builder.Property(document => document.Number).HasMaxLength(64).IsRequired();
            builder.Property(document => document.Holder).HasMaxLength(128).IsRequired();

            builder.HasIndex(document => document.OrderId);
            builder.HasIndex(document => document.TravellerId);
        }
    }
}
