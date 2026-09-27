using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm.Backoffice;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation.Backoffice;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve.Backoffice;
using AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate.Requests;
using AeroTech.Ordering.RestApi.V1._Shared;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AeroTech.Ordering.RestApi.V1.FulfillmentReservationAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Backoffice")]
    [Route($"Backoffice/v{{version:apiVersion}}/Orders/{{orderId:long}}/Reservations")]
    [Authorize(SurfaceAuthorization.Backoffice)]
    public sealed class BackofficeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BackofficeController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Reserve(long orderId, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeReserveOrderCommand(orderId), cancellationToken));

        [HttpPost("Services")]
        public async Task<IActionResult> ReserveServices(
            long orderId,
            [FromBody] ReserveServicesRequest request,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeReserveServicesCommand(orderId, request.OrderServiceIds), cancellationToken));

        [HttpPost("{reservationId:long}/Release")]
        public async Task<IActionResult> Release(long orderId, long reservationId, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeReleaseReservationCommand(orderId, reservationId), cancellationToken));

        [HttpPost("Confirmations")]
        public async Task<IActionResult> Confirm(
            long orderId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConfirmReservationsRequest? request,
            CancellationToken cancellationToken)
            => Ok(request?.ReservationIds is { Count: > 0 } reservationIds
                ? await _mediator.Send(new BackofficeConfirmReservationsCommand(orderId, reservationIds), cancellationToken)
                : await _mediator.Send(new BackofficeConfirmReservedCapacityCommand(orderId), cancellationToken));
    }
}
