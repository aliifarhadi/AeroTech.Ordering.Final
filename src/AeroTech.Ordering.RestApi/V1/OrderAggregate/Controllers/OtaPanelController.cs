using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.OtaPanel;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.OtaPanel;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.OtaPanel;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated.OtaPanel;
using AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests;
using AeroTech.Ordering.RestApi.V1._Shared;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("OTA Panel")]
    [Route($"OtaPanel/v{{version:apiVersion}}/Bookings")]
    [Authorize(SurfaceAuthorization.OtaPanel)]
    public sealed class OtaPanelController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OtaPanelController(IMediator mediator) => _mediator = mediator;

        [HttpPost("FlightOffers")]
        public async Task<IActionResult> CreateFromOffer(
            [FromBody] AgencyCreateOrderFromOfferRequest request,
            CancellationToken cancellationToken)
        {
            var command = new OtaPanelCreateOrderFromOfferCommand(
                request.OfferId,
                request.Contact,
                request.Travelers,
                request.SeatSelections ?? Array.Empty<SeatSelection>(),
                request.Remarks ?? Array.Empty<OrderRemarkInput>());

            return Ok(await _mediator.Send(command, cancellationToken));
        }

        [HttpGet("Paginated")]
        public async Task<IActionResult> Paginated(
            [FromQuery] OtaPanelGetOrdersPaginatedQuery query,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(query, cancellationToken));

        [HttpGet("{orderId:long}")]
        public async Task<IActionResult> GetById(long orderId, CancellationToken cancellationToken)
        {
            var order = await _mediator.Send(new OtaPanelGetOrderByIdQuery(orderId), cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpPost("{orderId:long}/Remarks")]
        public async Task<IActionResult> AddRemark(
            long orderId,
            [FromBody] AddRemarkRequest request,
            CancellationToken cancellationToken)
        {
            var command = new OtaPanelAddRemarkCommand(orderId, request.SupersedesRemarkId, request.Remark);

            return Ok(await _mediator.Send(command, cancellationToken));
        }
    }
}
