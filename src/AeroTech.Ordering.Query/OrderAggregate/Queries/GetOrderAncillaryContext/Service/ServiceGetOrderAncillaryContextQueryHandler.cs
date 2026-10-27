using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext.Service
{
    public sealed class ServiceGetOrderAncillaryContextQueryHandler : IRequestHandler<ServiceGetOrderAncillaryContextQuery, OrderAncillaryContextDto>
    {
        private readonly IGetOrderAncillaryContextService _service;

        public ServiceGetOrderAncillaryContextQueryHandler(IGetOrderAncillaryContextService service) => _service = service;

        public async Task<OrderAncillaryContextDto> Handle(ServiceGetOrderAncillaryContextQuery query, CancellationToken cancellationToken)
            => await _service.ExecuteAsync(query.OrderId, cancellationToken)
               ?? throw ExceptionFactory.OrderNotFound(query.OrderId);
    }
}
