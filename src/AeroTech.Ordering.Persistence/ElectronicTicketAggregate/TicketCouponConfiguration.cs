using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.ElectronicTicketAggregate
{
    public sealed class TicketCouponConfiguration : IEntityTypeConfiguration<TicketCoupon>
    {
        public void Configure(EntityTypeBuilder<TicketCoupon> builder)
        {
            builder.ToTable("TicketCoupons");
            builder.HasKey(coupon => coupon.Id);
            builder.Property(coupon => coupon.Id).ValueGeneratedNever();

            builder.Property(coupon => coupon.FareBasisSnapshot).HasMaxLength(64);
            builder.Property(coupon => coupon.BookingClassSnapshot).HasMaxLength(16);
            builder.Property(coupon => coupon.ProviderCouponStatusCode).HasMaxLength(16);
            builder.Property(coupon => coupon.UsageReference).HasMaxLength(100);

            builder.OwnsOne(coupon => coupon.IssuedSegment, segment =>
            {
                segment.Property(value => value.MarketingAirlineId).HasColumnName("IssuedMarketingAirlineId");
                segment.Property(value => value.OperatingAirlineId).HasColumnName("IssuedOperatingAirlineId");
                segment.Property(value => value.FlightNumber).HasColumnName("IssuedFlightNumber").HasMaxLength(16);
                segment.Property(value => value.OriginAirportId).HasColumnName("IssuedOriginAirportId");
                segment.Property(value => value.DestinationAirportId).HasColumnName("IssuedDestinationAirportId");
                segment.Property(value => value.DepartureDateTime).HasColumnName("IssuedDepartureDateTime");
                segment.Property(value => value.ArrivalDateTime).HasColumnName("IssuedArrivalDateTime");
                segment.Property(value => value.BookingClass).HasColumnName("IssuedBookingClass").HasMaxLength(16);
                segment.Property(value => value.RbdId).HasColumnName("IssuedRbdId");
                segment.Property(value => value.CabinClassId).HasColumnName("IssuedCabinClassId");
                segment.Property(value => value.SourceSegmentReference).HasColumnName("IssuedSourceSegmentReference").HasMaxLength(64);
            });
            builder.Navigation(coupon => coupon.IssuedSegment).IsRequired();

            builder.OwnsOne(coupon => coupon.BaggageAllowanceSnapshot, allowance =>
            {
                allowance.Property(value => value.Pieces).HasColumnName("BaggageAllowancePieces");
                allowance.Property(value => value.Weight).HasColumnName("BaggageAllowanceWeight");
                allowance.Property(value => value.Unit).HasColumnName("BaggageAllowanceUnit");
            });

            builder.HasIndex(coupon => new { coupon.TicketId, coupon.CouponNumber }).IsUnique();
            builder.HasIndex(coupon => coupon.CurrentOrderServiceId);
            builder.HasIndex(coupon => coupon.OrderSegmentId);
        }
    }
}
