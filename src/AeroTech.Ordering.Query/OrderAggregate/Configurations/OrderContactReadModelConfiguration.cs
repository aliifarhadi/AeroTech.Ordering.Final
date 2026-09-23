using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.OrderAggregate.Configurations
{
    public sealed class OrderContactReadModelConfiguration : IEntityTypeConfiguration<OrderContactReadModel>
    {
        public void Configure(EntityTypeBuilder<OrderContactReadModel> builder)
        {
            builder.ToTable("OrderContacts");
            builder.HasKey(contact => contact.Id);
            builder.Property(contact => contact.Id).ValueGeneratedNever();

            builder.Property(contact => contact.ContactName).HasMaxLength(128);

            builder.HasIndex(contact => new { contact.OrderId, contact.Sequence }).IsUnique();
        }
    }
}
