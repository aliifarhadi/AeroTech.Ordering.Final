using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderAssistedTravelServiceConfiguration : IEntityTypeConfiguration<OrderAssistedTravelService>
    {
        public void Configure(EntityTypeBuilder<OrderAssistedTravelService> builder)
        {
            builder.ToTable("OrderAssistedTravelServices");

            builder.Property(service => service.AssistanceSsrCode).HasMaxLength(16);

            builder.Ignore(service => service.MedicalEquipment);
            builder.Ignore(service => service.UnaccompaniedMinor);
        }
    }
}
