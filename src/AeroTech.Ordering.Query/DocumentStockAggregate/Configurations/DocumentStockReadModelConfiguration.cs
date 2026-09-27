using AeroTech.Ordering.Query.DocumentStockAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Configurations
{
    public sealed class DocumentStockReadModelConfiguration : IEntityTypeConfiguration<DocumentStockReadModel>
    {
        public void Configure(EntityTypeBuilder<DocumentStockReadModel> builder)
        {
            builder.ToTable("DocumentStocks");
            builder.HasKey(stock => stock.Id);
            builder.Property(stock => stock.Id).ValueGeneratedNever();

            builder.Property(stock => stock.Prefix).HasMaxLength(32).IsRequired();
            builder.Property(stock => stock.CheckDigitProfile).HasMaxLength(32).IsRequired();

            builder.HasIndex(stock => new { stock.OwnerAirlineId, stock.DocumentKind });
        }
    }
}
