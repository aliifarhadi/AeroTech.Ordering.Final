using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.OtaPanel
{
    public sealed class OtaPanelGetOrderByIdQueryHandler : IRequestHandler<OtaPanelGetOrderByIdQuery, FlightOrderDto?>
    {
        private readonly IGetFlightOrderByIdService _service;
        private readonly ICallerCustomer _callerCustomer;

        public OtaPanelGetOrderByIdQueryHandler(IGetFlightOrderByIdService service, ICallerCustomer callerCustomer)
        {
            _service = service;
            _callerCustomer = callerCustomer;
        }

        public Task<FlightOrderDto?> Handle(OtaPanelGetOrderByIdQuery query, CancellationToken cancellationToken)
            => _service.ExecuteAsync(query.OrderId, OrderQueryScope.OwnedBy(_callerCustomer.RequiredId()), cancellationToken);
    }
}
