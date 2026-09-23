using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public interface IGetOrderByIdService
    {
        Task<OrderDetailDto?> ExecuteAsync(
            long orderId,
            OrderQueryScope scope,
            CancellationToken cancellationToken = default);
    }
}
