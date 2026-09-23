using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderChangeConfiguration : IEntityTypeConfiguration<OrderChange>
    {
        public void Configure(EntityTypeBuilder<OrderChange> builder)
        {
            builder.ToTable("OrderChanges");
            builder.HasKey(change => change.Id);
            builder.Property(change => change.Id).ValueGeneratedNever();

            builder.Property(change => change.SourceReference).HasMaxLength(512);

            builder.OwnsOne(change => change.ActorContext, actorContext =>
            {
                actorContext.Property(value => value.Channel).HasColumnName("ActorChannel");
                actorContext.Property(value => value.ContextType).HasColumnName("ActorContextType");
                actorContext.Property(value => value.PrincipalType).HasColumnName("ActorPrincipalType");
                actorContext.Property(value => value.ActorId).HasColumnName("ActorId");
                actorContext.Property(value => value.AirlineUserId).HasColumnName("ActorAirlineUserId");
                actorContext.Property(value => value.TravelAgencyId).HasColumnName("ActorTravelAgencyId");
                actorContext.Property(value => value.TravelAgencyUserId).HasColumnName("ActorTravelAgencyUserId");
                actorContext.Property(value => value.IndividualId).HasColumnName("ActorIndividualId");
                actorContext.Property(value => value.PartnerApiAccessProfileId).HasColumnName("ActorPartnerApiAccessProfileId");
                actorContext.Property(value => value.OfficeKind).HasColumnName("ActorOfficeKind");
                actorContext.Property(value => value.OfficeId).HasColumnName("ActorOfficeId");
            });
            builder.Navigation(change => change.ActorContext).IsRequired();

            builder.HasIndex(change => new { change.OrderId, change.CommercialVersion });
        }
    }
}
