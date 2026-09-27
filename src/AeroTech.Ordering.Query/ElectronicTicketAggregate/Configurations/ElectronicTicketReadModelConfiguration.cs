using AeroTech.Ordering.Query.ElectronicTicketAggregate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Query.ElectronicTicketAggregate.Configurations
{
    public sealed class ElectronicTicketReadModelConfiguration : IEntityTypeConfiguration<ElectronicTicketReadModel>
    {
        public void Configure(EntityTypeBuilder<ElectronicTicketReadModel> builder)
        {
            builder.ToTable("ElectronicTickets");
            builder.HasKey(ticket => ticket.Id);
            builder.Property(ticket => ticket.Id).ValueGeneratedNever();

            builder.Property(ticket => ticket.DocumentNumber).HasMaxLength(32).IsRequired();

            builder.HasIndex(ticket => ticket.OrderId);
            builder.HasIndex(ticket => ticket.DocumentNumber).IsUnique();
        }
    }
}
