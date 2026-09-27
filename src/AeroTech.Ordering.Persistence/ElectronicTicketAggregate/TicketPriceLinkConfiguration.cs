using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.ElectronicTicketAggregate
{
    public sealed class TicketPriceLinkConfiguration : IEntityTypeConfiguration<TicketPriceLink>
    {
        public void Configure(EntityTypeBuilder<TicketPriceLink> builder)
        {
            builder.ToTable("TicketPriceLinks");
            builder.HasKey(link => link.Id);
            builder.Property(link => link.Id).ValueGeneratedNever();

            builder.HasIndex(link => link.PricingLineId);
            builder.HasIndex(link => link.PricingAllocationId);
            builder.HasIndex(link => link.TicketCouponId);
        }
    }
}
