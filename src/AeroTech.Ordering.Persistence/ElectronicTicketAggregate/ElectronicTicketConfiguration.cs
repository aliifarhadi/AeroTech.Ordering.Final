using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.ElectronicTicketAggregate
{
    public sealed class ElectronicTicketConfiguration : IEntityTypeConfiguration<ElectronicTicket>
    {
        public void Configure(EntityTypeBuilder<ElectronicTicket> builder)
        {
            builder.ToTable("ElectronicTickets");
            builder.HasKey(ticket => ticket.Id);
            builder.Property(ticket => ticket.Id).ValueGeneratedNever();

            builder.Property(ticket => ticket.DocumentNumber).HasMaxLength(32).IsRequired();
            builder.Property(ticket => ticket.ProviderReference).HasMaxLength(100);

            builder.OwnsOne(ticket => ticket.IssuanceContext, context =>
            {
                context.Property(value => value.IssuerCarrierId).HasColumnName("IssuerCarrierId");
                context.Property(value => value.ValidatingCarrierId).HasColumnName("ValidatingCarrierId");
                context.Property(value => value.IssuingOfficeId).HasColumnName("IssuingOfficeId");
                context.Property(value => value.IssuedByActorId).HasColumnName("IssuedByActorId");
                context.Property(value => value.TravelAgencyId).HasColumnName("TravelAgencyId");
                context.Property(value => value.AgencyIataNumber).HasColumnName("AgencyIataNumber").HasMaxLength(16);
                context.Property(value => value.Pcc).HasColumnName("Pcc").HasMaxLength(16);
                context.Property(value => value.SalesChannel).HasColumnName("SalesChannel");
                context.Property(value => value.SourceFormOfPaymentCode).HasColumnName("SourceFormOfPaymentCode").HasMaxLength(32);
            });
            builder.Navigation(ticket => ticket.IssuanceContext).IsRequired();

            builder.HasMany(ticket => ticket.Coupons)
                .WithOne()
                .HasForeignKey(coupon => coupon.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(ticket => ticket.PriceLinks)
                .WithOne()
                .HasForeignKey(link => link.ElectronicTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(ticket => ticket.Coupons).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(ticket => ticket.PriceLinks).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(ticket => ticket.DocumentNumber).IsUnique();
            builder.HasIndex(ticket => ticket.OriginalOrderId);
            builder.HasIndex(ticket => ticket.CurrentServicingOrderId);
            builder.HasIndex(ticket => ticket.TravellerId);
            builder.HasIndex(ticket => ticket.IssueFulfillmentTaskId);
        }
    }
}
