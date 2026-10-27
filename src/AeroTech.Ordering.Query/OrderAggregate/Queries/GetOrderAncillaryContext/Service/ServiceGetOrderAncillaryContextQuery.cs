using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext.Service
{
    public sealed record ServiceGetOrderAncillaryContextQuery(long OrderId) : IRequest<OrderAncillaryContextDto>;
}
