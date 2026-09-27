using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record VoidDocumentsRequest(IReadOnlyList<ElectronicTicketVoidTarget> Tickets, VoidReason Reason, string? ReasonDetail);
}
