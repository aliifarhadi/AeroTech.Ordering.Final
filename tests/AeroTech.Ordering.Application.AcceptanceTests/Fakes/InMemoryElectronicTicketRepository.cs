using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class InMemoryElectronicTicketRepository(InMemoryUnitOfWork unitOfWork) : IElectronicTicketRepository
{
    private readonly List<ElectronicTicket> _committed = [];

    public IReadOnlyList<ElectronicTicket> Committed => _committed;

    public Task AddAsync(ElectronicTicket ticket, CancellationToken cancellationToken = default)
    {
        unitOfWork.Stage(() => _committed.Add(ticket));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ElectronicTicket>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ElectronicTicket>>(_committed.Where(ticket => ticket.CurrentServicingOrderId == orderId).ToList());

    public Task<bool> AnyWithDocumentNumberAsync(IReadOnlyCollection<string> documentNumbers, CancellationToken cancellationToken = default)
        => Task.FromResult(_committed.Any(ticket => documentNumbers.Contains(ticket.DocumentNumber)));
}
