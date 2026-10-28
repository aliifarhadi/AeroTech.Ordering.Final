using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderSeatServiceConfiguration : IEntityTypeConfiguration<OrderSeatService>
    {
        public void Configure(EntityTypeBuilder<OrderSeatService> builder)
        {
            builder.ToTable("OrderSeatServices");

            builder.Property(service => service.SeatNumber).HasMaxLength(16).IsRequired();
            builder.Property(service => service.VariantCode).HasMaxLength(8);

            builder.OwnsOne(service => service.Source, source => AncillarySaleTermsMapping.MapSource(source));
            builder.OwnsOne(service => service.Definition, definition => AncillarySaleTermsMapping.MapDefinition(definition));
            builder.OwnsOne(service => service.Provision, provision => AncillarySaleTermsMapping.MapProvision(provision));
            builder.OwnsOne(service => service.Pricing, pricing => AncillarySaleTermsMapping.MapPricing(pricing));

            builder.Ignore(service => service.IsPricedSelection);

            builder.HasIndex(service => service.SegmentId);
            builder.HasIndex(service => service.AssociatedAirServiceId);
        }
    }
}
