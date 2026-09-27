using AeroTech.Ordering.Domain.DocumentStockAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.DocumentStockAggregate
{
    public sealed class DocumentStockConfiguration : IEntityTypeConfiguration<DocumentStock>
    {
        public void Configure(EntityTypeBuilder<DocumentStock> builder)
        {
            builder.ToTable("DocumentStocks");
            builder.HasKey(stock => stock.Id);
            builder.Property(stock => stock.Id).ValueGeneratedNever();

            builder.Property(stock => stock.Prefix).HasMaxLength(32).IsRequired();
            builder.Property(stock => stock.CheckDigitProfile).HasMaxLength(32).IsRequired();

            builder.Ignore(stock => stock.RemainingNumbers);

            builder.HasMany(stock => stock.Allocations)
                .WithOne()
                .HasForeignKey(allocation => allocation.DocumentStockId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(stock => stock.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(stock => new { stock.OwnerAirlineId, stock.DocumentKind, stock.Prefix });
        }
    }
}
