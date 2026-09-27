using AeroTech.Framework.Core.ServiceContracts;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts
{
    public interface IElectronicTicketQueryDbSynchronizer : IQueryDbSynchronizer
    {
        Task ProjectIssuedAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken = default);
    }
}
