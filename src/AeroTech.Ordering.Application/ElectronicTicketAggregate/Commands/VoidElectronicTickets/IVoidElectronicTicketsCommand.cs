using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public interface IVoidElectronicTicketsCommand
    {
        long OrderId { get; }

        IReadOnlyList<ElectronicTicketVoidTarget> Tickets { get; }

        VoidReason Reason { get; }

        string? ReasonDetail { get; }
    }
}
