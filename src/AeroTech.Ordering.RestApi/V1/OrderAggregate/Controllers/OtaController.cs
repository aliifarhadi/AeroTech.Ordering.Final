using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Ota;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Ota;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Ota;
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
    [Tags("OTA Api")]
    [Route($"Api/v{{version:apiVersion}}/Bookings")]
    [Authorize(SurfaceAuthorization.Ota)]
    public sealed class OtaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OtaController(IMediator mediator) => _mediator = mediator;

        [HttpPost("FlightOffers")]
        public async Task<IActionResult> CreateFromOffer(
            [FromBody] AgencyCreateOrderFromOfferRequest request,
            CancellationToken cancellationToken)
        {
            var command = new OtaCreateOrderFromOfferCommand(
                request.OfferId,
                request.Contact,
                request.Travelers,
                request.SeatSelections ?? Array.Empty<SeatSelection>(),
                request.Remarks ?? Array.Empty<OrderRemarkInput>());

            return Ok(await _mediator.Send(command, cancellationToken));
        }

        [HttpGet("{orderId:long}")]
        public async Task<IActionResult> GetById(long orderId, CancellationToken cancellationToken)
        {
            var order = await _mediator.Send(new OtaGetOrderByIdQuery(orderId), cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpPost("{orderId:long}/Remarks")]
        public async Task<IActionResult> AddRemark(
            long orderId,
            [FromBody] AddRemarkRequest request,
            CancellationToken cancellationToken)
        {
            var command = new OtaAddRemarkCommand(orderId, request.SupersedesRemarkId, request.Remark);

            return Ok(await _mediator.Send(command, cancellationToken));
        }
    }
}
