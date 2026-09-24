using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderFarePricingUnitConfiguration : IEntityTypeConfiguration<OrderFarePricingUnit>
    {
        public void Configure(EntityTypeBuilder<OrderFarePricingUnit> builder)
        {
            builder.ToTable("OrderFarePricingUnits");
            builder.HasKey(pricingUnit => pricingUnit.Id);
            builder.Property(pricingUnit => pricingUnit.Id).ValueGeneratedNever();

            builder.PrimitiveCollection(pricingUnit => pricingUnit.CoveredJourneyIds)
                .HasField("_coveredJourneyIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            builder.HasMany(pricingUnit => pricingUnit.FareComponents)
                .WithOne()
                .HasForeignKey(component => component.OrderFarePricingUnitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(pricingUnit => pricingUnit.FareComponents).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(pricingUnit => new { pricingUnit.OrderId, pricingUnit.Sequence }).IsUnique();
        }
    }
}
