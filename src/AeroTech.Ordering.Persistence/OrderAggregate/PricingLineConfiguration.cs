using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class PricingLineConfiguration : IEntityTypeConfiguration<PricingLine>
    {
        public void Configure(EntityTypeBuilder<PricingLine> builder)
        {
            builder.ToTable("PricingLines");
            builder.HasKey(line => line.Id);
            builder.Property(line => line.Id).ValueGeneratedNever();

            builder.Property(line => line.Code).HasMaxLength(32);
            builder.Property(line => line.Description).HasMaxLength(256);
            builder.Property(line => line.Reference).HasMaxLength(64);

            builder.Ignore(line => line.SignedEquivalentAmount);

            builder.OwnsOne(line => line.ExchangeRateSnapshot, snapshot =>
            {
                snapshot.Property(value => value.FromCurrencyId).HasColumnName("ExchangeFromCurrencyId");
                snapshot.Property(value => value.ToCurrencyId).HasColumnName("ExchangeToCurrencyId");
                snapshot.Property(value => value.Rate).HasColumnName("ExchangeRate").HasPrecision(19, 9);
                snapshot.Property(value => value.DecimalPlaces).HasColumnName("ExchangeDecimalPlaces");
                snapshot.Property(value => value.PeriodId).HasColumnName("ExchangePeriodId").HasMaxLength(64);
            });

            builder.HasMany(line => line.Allocations)
                .WithOne()
                .HasForeignKey(allocation => allocation.PricingLineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(line => line.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(line => new { line.OrderId, line.Treatment });
            builder.HasIndex(line => line.CreatedByChangeId);
        }
    }
}
