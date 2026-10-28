using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderMealServiceConfiguration : IEntityTypeConfiguration<OrderMealService>
    {
        public void Configure(EntityTypeBuilder<OrderMealService> builder)
        {
            builder.ToTable("OrderMealServices");

            builder.Property(service => service.MealCode).HasMaxLength(16);
            builder.Property(service => service.MenuItemRef).HasMaxLength(128);
            builder.Property(service => service.DietaryCode).HasMaxLength(16);
            builder.Property(service => service.ExclusiveMealFamilyCode).HasMaxLength(64);
        }
    }
}
