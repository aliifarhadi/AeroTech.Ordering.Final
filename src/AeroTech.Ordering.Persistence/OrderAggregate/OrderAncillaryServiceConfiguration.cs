using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderAncillaryServiceConfiguration : IEntityTypeConfiguration<OrderAncillaryService>
    {
        public void Configure(EntityTypeBuilder<OrderAncillaryService> builder)
        {
            builder.ToTable("OrderAncillaryServices");

            builder.Property(service => service.VariantCode).HasMaxLength(8).IsRequired();

            builder.PrimitiveCollection(service => service.CoveredAirServiceIds)
                .HasField("_coveredAirServiceIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            builder.OwnsOne(service => service.Source, source => AncillarySaleTermsMapping.MapSource(source));
            builder.OwnsOne(service => service.Definition, definition => AncillarySaleTermsMapping.MapDefinition(definition));
            builder.OwnsOne(service => service.Provision, provision => AncillarySaleTermsMapping.MapProvision(provision));
            builder.OwnsOne(service => service.Pricing, pricing => AncillarySaleTermsMapping.MapPricing(pricing));

            builder.Navigation(service => service.Source).IsRequired();
            builder.Navigation(service => service.Definition).IsRequired();
            builder.Navigation(service => service.Provision).IsRequired();
            builder.Navigation(service => service.Pricing).IsRequired();
        }
    }
}
