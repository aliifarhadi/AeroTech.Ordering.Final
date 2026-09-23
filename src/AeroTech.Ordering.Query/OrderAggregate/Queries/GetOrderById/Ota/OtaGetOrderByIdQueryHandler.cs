using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Ota
{
    public sealed class OtaGetOrderByIdQueryHandler : IRequestHandler<OtaGetOrderByIdQuery, FlightOrderDto?>
    {
        private readonly IGetFlightOrderByIdService _service;
        private readonly ICallerCustomer _callerCustomer;

        public OtaGetOrderByIdQueryHandler(IGetFlightOrderByIdService service, ICallerCustomer callerCustomer)
        {
            _service = service;
            _callerCustomer = callerCustomer;
        }

        public Task<FlightOrderDto?> Handle(OtaGetOrderByIdQuery query, CancellationToken cancellationToken)
            => _service.ExecuteAsync(query.OrderId, OrderQueryScope.OwnedBy(_callerCustomer.RequiredId()), cancellationToken);
    }
}
