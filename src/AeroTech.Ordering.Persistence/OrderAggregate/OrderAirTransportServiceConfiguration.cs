using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderAirTransportServiceConfiguration : IEntityTypeConfiguration<OrderAirTransportService>
    {
        public void Configure(EntityTypeBuilder<OrderAirTransportService> builder)
        {
            builder.ToTable("OrderAirTransportServices");

            builder.Property(service => service.BookingClass).HasMaxLength(16);
            builder.Property(service => service.FareBasis).HasMaxLength(64);
            builder.Property(service => service.FareFamily).HasMaxLength(128);
            builder.Property(service => service.FareType).HasMaxLength(64);

            builder.OwnsOne(service => service.CheckedBaggageAllowance, allowance =>
            {
                allowance.Property(value => value.Pieces).HasColumnName("CheckedBaggagePieces");
                allowance.Property(value => value.Weight).HasColumnName("CheckedBaggageWeight");
                allowance.Property(value => value.Unit).HasColumnName("CheckedBaggageUnit");
            });

            builder.OwnsOne(service => service.CabinBaggageAllowance, allowance =>
            {
                allowance.Property(value => value.Pieces).HasColumnName("CabinBaggagePieces");
                allowance.Property(value => value.Weight).HasColumnName("CabinBaggageWeight");
                allowance.Property(value => value.Unit).HasColumnName("CabinBaggageUnit");
            });

            builder.HasIndex(service => service.SegmentId);
        }
    }
}
