using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.OtaPanel
{
    public sealed record OtaPanelGetOrderByIdQuery(long OrderId) : IRequest<FlightOrderDto?>;
}
