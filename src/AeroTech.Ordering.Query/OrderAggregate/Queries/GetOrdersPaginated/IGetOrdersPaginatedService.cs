using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated
{
    public interface IGetOrdersPaginatedService
    {
        Task<GridData<OrderPaginatedRowDto>> ExecuteAsync(
            IOrdersPaginatedQuery query,
            OrderQueryScope scope,
            CancellationToken cancellationToken = default);
    }
}
