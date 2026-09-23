using AeroTech.Framework.Core.ServiceContracts;

namespace AeroTech.Ordering.Domain.OrderAggregate.Contracts
{
    public interface IOrderQueryDbSynchronizer : IQueryDbSynchronizer
    {
        Task ProjectCreatedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default);

        Task ProjectRemarkedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default);
    }
}
