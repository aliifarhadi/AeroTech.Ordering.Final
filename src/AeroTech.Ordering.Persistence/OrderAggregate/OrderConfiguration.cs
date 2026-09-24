using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.Id).ValueGeneratedNever();

            builder.Property(order => order.SourceOfferId).HasMaxLength(512).IsRequired();
            builder.Property(order => order.RecordLocator).HasMaxLength(6);

            builder.HasIndex(order => order.OrderReference).IsUnique();
            builder.HasIndex(order => order.RecordLocator).IsUnique().HasFilter("[RecordLocator] IS NOT NULL");
            builder.HasIndex(order => order.CustomerId);
            builder.HasIndex(order => order.Status);
            builder.HasIndex(order => order.LastTicketingDate);

            builder.OwnsOne(order => order.SalesContext, salesContext =>
            {
                salesContext.Property(value => value.Channel).HasColumnName("SalesChannel");
                salesContext.Property(value => value.ContextType).HasColumnName("SalesContextType");
                salesContext.Property(value => value.PrincipalType).HasColumnName("SalesPrincipalType");
                salesContext.Property(value => value.ActorId).HasColumnName("SalesActorId");
                salesContext.Property(value => value.AirlineUserId).HasColumnName("SalesAirlineUserId");
                salesContext.Property(value => value.TravelAgencyId).HasColumnName("SalesTravelAgencyId");
                salesContext.Property(value => value.TravelAgencyUserId).HasColumnName("SalesTravelAgencyUserId");
                salesContext.Property(value => value.IndividualId).HasColumnName("SalesIndividualId");
                salesContext.Property(value => value.PartnerApiAccessProfileId).HasColumnName("SalesPartnerApiAccessProfileId");
                salesContext.Property(value => value.OfficeKind).HasColumnName("SalesOfficeKind");
                salesContext.Property(value => value.OfficeId).HasColumnName("SalesOfficeId");
            });
            builder.Navigation(order => order.SalesContext).IsRequired();

            builder.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Services).WithOne().HasForeignKey(service => service.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Travellers).WithOne().HasForeignKey(traveller => traveller.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Contacts).WithOne().HasForeignKey(contact => contact.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Journeys).WithOne().HasForeignKey(journey => journey.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Segments).WithOne().HasForeignKey(segment => segment.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.PricingLines).WithOne().HasForeignKey(line => line.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.FarePricingUnits).WithOne().HasForeignKey(pricingUnit => pricingUnit.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Changes).WithOne().HasForeignKey(change => change.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(order => order.Remarks).WithOne().HasForeignKey(remark => remark.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Services).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Travellers).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Journeys).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.PricingLines).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.FarePricingUnits).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Changes).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(order => order.Remarks).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
