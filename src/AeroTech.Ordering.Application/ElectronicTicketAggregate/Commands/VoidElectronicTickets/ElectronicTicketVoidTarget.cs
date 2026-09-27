namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public sealed record ElectronicTicketVoidTarget(long ElectronicTicketId, int ExpectedDocumentVersion);
}
