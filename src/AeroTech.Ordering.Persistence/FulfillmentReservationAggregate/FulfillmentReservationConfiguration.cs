using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.FulfillmentReservationAggregate
{
    public sealed class FulfillmentReservationConfiguration : IEntityTypeConfiguration<FulfillmentReservation>
    {
        public void Configure(EntityTypeBuilder<FulfillmentReservation> builder)
        {
            builder.ToTable("FulfillmentReservations");
            builder.HasKey(reservation => reservation.Id);
            builder.Property(reservation => reservation.Id).ValueGeneratedNever();

            builder.Property(reservation => reservation.FulfillmentProviderKey).HasMaxLength(64).IsRequired();
            builder.Property(reservation => reservation.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(reservation => reservation.CorrelationReference).HasMaxLength(100).IsRequired();
            builder.Property(reservation => reservation.ProviderOperationRef).HasMaxLength(100);

            builder.OwnsOne(reservation => reservation.ValidationEvidence, evidence =>
            {
                evidence.Property(value => value.CommercialVersion).HasColumnName("ValidationCommercialVersion");
                evidence.Property(value => value.ValidUntil).HasColumnName("ValidationValidUntil");
                evidence.Property(value => value.ValidatedAt).HasColumnName("ValidationValidatedAt");
                evidence.PrimitiveCollection(value => value.ValidatedOrderServiceIds)
                    .HasField("_validatedOrderServiceIds")
                    .UsePropertyAccessMode(PropertyAccessMode.Field)
                    .HasColumnName("ValidatedOrderServiceIds")
                    .IsRequired();
            });

            builder.Ignore(reservation => reservation.IsUnresolved);
            builder.Ignore(reservation => reservation.CoveredOrderServiceIds);

            builder.HasMany(reservation => reservation.Units)
                .WithOne()
                .HasForeignKey(unit => unit.FulfillmentReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(reservation => reservation.Units).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(reservation => reservation.OrderId);
            builder.HasIndex(reservation => reservation.Status);
            builder.HasIndex(reservation => reservation.IdempotencyKey).IsUnique();
        }
    }
}
