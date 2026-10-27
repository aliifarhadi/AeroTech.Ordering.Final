using AeroTech.Ordering.Query.OrderAggregate.Dto;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext
{
    public interface IGetOrderAncillaryContextService
    {
        Task<OrderAncillaryContextDto?> ExecuteAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
