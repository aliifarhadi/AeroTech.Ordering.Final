using AeroTech.Ordering.Query.ElectronicTicketAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.ElectronicTicketAggregate.Configurations
{
    public sealed class TicketCouponReadModelConfiguration : IEntityTypeConfiguration<TicketCouponReadModel>
    {
        public void Configure(EntityTypeBuilder<TicketCouponReadModel> builder)
        {
            builder.ToTable("TicketCoupons");
            builder.HasKey(coupon => coupon.Id);
            builder.Property(coupon => coupon.Id).ValueGeneratedNever();

            builder.Property(coupon => coupon.FareBasisSnapshot).HasMaxLength(64);
            builder.Property(coupon => coupon.BookingClassSnapshot).HasMaxLength(16);

            builder.HasIndex(coupon => coupon.OrderId);
            builder.HasIndex(coupon => coupon.ElectronicTicketId);
        }
    }
}
