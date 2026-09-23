using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Backoffice
{
    public sealed class BackofficeGetOrderByIdQueryHandler : IRequestHandler<BackofficeGetOrderByIdQuery, BackofficeOrderDto?>
    {
        private readonly IGetBackofficeOrderByIdService _service;

        public BackofficeGetOrderByIdQueryHandler(IGetBackofficeOrderByIdService service) => _service = service;

        public Task<BackofficeOrderDto?> Handle(BackofficeGetOrderByIdQuery query, CancellationToken cancellationToken)
            => _service.ExecuteAsync(query.OrderId, cancellationToken);
    }
}
