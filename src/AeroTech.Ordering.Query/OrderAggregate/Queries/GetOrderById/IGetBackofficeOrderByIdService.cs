using AeroTech.Ordering.Query.OrderAggregate.Dto;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public interface IGetBackofficeOrderByIdService
    {
        Task<BackofficeOrderDto?> ExecuteAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
