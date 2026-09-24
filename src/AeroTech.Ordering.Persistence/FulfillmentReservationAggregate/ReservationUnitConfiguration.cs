using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentReservationAggregate
{
    public sealed class ReservationUnitConfiguration : IEntityTypeConfiguration<ReservationUnit>
    {
        public void Configure(EntityTypeBuilder<ReservationUnit> builder)
        {
            builder.ToTable("ReservationUnits");
            builder.HasKey(unit => unit.Id);
            builder.Property(unit => unit.Id).ValueGeneratedNever();

            builder.Property(unit => unit.UnitCorrelationKey).HasMaxLength(100).IsRequired();
            builder.Property(unit => unit.ProviderUnitRef).HasMaxLength(100);
            builder.Property(unit => unit.RawStatusCode).HasMaxLength(32);
            builder.Property(unit => unit.ObservedSeat).HasMaxLength(10);

            builder.PrimitiveCollection(unit => unit.OrderServiceIds)
                .HasField("_orderServiceIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .IsRequired();

            builder.HasIndex(unit => new { unit.FulfillmentReservationId, unit.UnitCorrelationKey }).IsUnique();
        }
    }
}
