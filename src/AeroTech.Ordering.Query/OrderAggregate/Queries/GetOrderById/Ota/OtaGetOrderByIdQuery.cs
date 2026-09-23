using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Ota
{
    public sealed record OtaGetOrderByIdQuery(long OrderId) : IRequest<FlightOrderDto?>;
}
