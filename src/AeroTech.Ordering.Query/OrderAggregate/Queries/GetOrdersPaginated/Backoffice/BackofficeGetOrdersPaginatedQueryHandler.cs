using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated.Backoffice
{
    public sealed class BackofficeGetOrdersPaginatedQueryHandler
        : IRequestHandler<BackofficeGetOrdersPaginatedQuery, GridData<OrderPaginatedRowDto>>
    {
        private readonly IGetOrdersPaginatedService _service;

        public BackofficeGetOrdersPaginatedQueryHandler(IGetOrdersPaginatedService service) => _service = service;

        public Task<GridData<OrderPaginatedRowDto>> Handle(
            BackofficeGetOrdersPaginatedQuery query,
            CancellationToken cancellationToken)
            => _service.ExecuteAsync(
                query,
                query.CustomerId is { } customerId ? OrderQueryScope.OwnedBy(customerId) : OrderQueryScope.Unrestricted,
                cancellationToken);
    }
}
