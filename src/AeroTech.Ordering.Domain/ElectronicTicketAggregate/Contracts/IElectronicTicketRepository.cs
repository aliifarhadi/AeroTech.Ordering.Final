namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts
{
    public interface IElectronicTicketRepository
    {
        Task AddAsync(ElectronicTicket ticket, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ElectronicTicket>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default);

        Task<bool> AnyWithDocumentNumberAsync(IReadOnlyCollection<string> documentNumbers, CancellationToken cancellationToken = default);
    }
}
