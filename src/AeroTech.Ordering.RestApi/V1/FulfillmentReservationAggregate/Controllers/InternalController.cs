using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Internal;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Internal;
using AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate.Requests;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Internal")]
    [Route($"Internal/v{{version:apiVersion}}/Orders/{{orderId:long}}/Reservations")]
    public sealed class InternalController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InternalController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Reserve(long orderId, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new InternalReserveOrderCommand(orderId), cancellationToken));

        [HttpPost("Services")]
        public async Task<IActionResult> ReserveServices(
            long orderId,
            [FromBody] ReserveServicesRequest request,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new InternalReserveServicesCommand(orderId, request.OrderServiceIds), cancellationToken));

        [HttpPost("{reservationId:long}/Release")]
        public async Task<IActionResult> Release(long orderId, long reservationId, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new InternalReleaseReservationCommand(orderId, reservationId), cancellationToken));
    }
}
