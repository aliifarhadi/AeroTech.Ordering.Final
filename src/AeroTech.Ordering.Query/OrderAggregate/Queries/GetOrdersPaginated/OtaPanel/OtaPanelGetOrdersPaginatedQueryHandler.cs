using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated.OtaPanel
{
    public sealed class OtaPanelGetOrdersPaginatedQueryHandler
        : IRequestHandler<OtaPanelGetOrdersPaginatedQuery, GridData<OrderPaginatedRowDto>>
    {
        private readonly IGetOrdersPaginatedService _service;
        private readonly ICallerCustomer _callerCustomer;

        public OtaPanelGetOrdersPaginatedQueryHandler(IGetOrdersPaginatedService service, ICallerCustomer callerCustomer)
        {
            _service = service;
            _callerCustomer = callerCustomer;
        }

        public Task<GridData<OrderPaginatedRowDto>> Handle(
            OtaPanelGetOrdersPaginatedQuery query,
            CancellationToken cancellationToken)
            => _service.ExecuteAsync(query, OrderQueryScope.OwnedBy(_callerCustomer.RequiredId()), cancellationToken);
    }
}
