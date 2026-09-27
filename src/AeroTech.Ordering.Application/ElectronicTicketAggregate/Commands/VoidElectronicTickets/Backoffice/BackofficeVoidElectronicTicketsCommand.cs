using AeroTech.Messages.Ordering.Enums;
using MediatR;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets.Backoffice
{
    public sealed record BackofficeVoidElectronicTicketsCommand(
        long OrderId,
        IReadOnlyList<ElectronicTicketVoidTarget> Tickets,
        VoidReason Reason,
        string? ReasonDetail) : IRequest<VoidElectronicTicketsResult>, IVoidElectronicTicketsCommand;
}
