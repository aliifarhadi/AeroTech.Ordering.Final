using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Backoffice
{
    public sealed record BackofficeGetOrderByIdQuery(long OrderId) : IRequest<BackofficeOrderDto?>;
}
