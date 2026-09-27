using AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder.Backoffice;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById.Backoffice;
using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated.Backoffice;
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
    [Tags("Backoffice")]
    [Route($"Backoffice/v{{version:apiVersion}}/Orders")]
    [Authorize(SurfaceAuthorization.Backoffice)]
    public sealed class BackofficeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BackofficeController(IMediator mediator) => _mediator = mediator;

        [HttpPost("FlightOffers")]
        public async Task<IActionResult> CreateFromOffer(
            [FromBody] BackofficeCreateOrderFromOfferRequest request,
            CancellationToken cancellationToken)
        {
            var command = new BackofficeCreateOrderFromOfferCommand(
                request.OfferId,
                request.CustomerId,
                request.Contact,
                request.Travelers,
                request.SeatSelections ?? Array.Empty<SeatSelection>(),
                request.Remarks ?? Array.Empty<OrderRemarkInput>());

            return Ok(await _mediator.Send(command, cancellationToken));
        }

        [HttpGet("Paginated")]
        public async Task<IActionResult> Paginated(
            [FromQuery] BackofficeGetOrdersPaginatedQuery query,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(query, cancellationToken));

        [HttpGet("{orderId:long}")]
        public async Task<IActionResult> GetById(long orderId, CancellationToken cancellationToken)
        {
            var order = await _mediator.Send(new BackofficeGetOrderByIdQuery(orderId), cancellationToken);

            return order is null ? NotFound() : Ok(order);
        }

        [HttpPost("{orderId:long}/Remarks")]
        public async Task<IActionResult> AddRemark(
            long orderId,
            [FromBody] AddRemarkRequest request,
            CancellationToken cancellationToken)
        {
            var command = new BackofficeAddRemarkCommand(orderId, request.SupersedesRemarkId, request.Remark);

            return Ok(await _mediator.Send(command, cancellationToken));
        }

        [HttpPost("{orderId:long}/Issuance")]
        public async Task<IActionResult> Issue(
            long orderId,
            [FromBody] IssueOrderRequest request,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeIssueOrderCommand(orderId, request.TicketDocumentStockId), cancellationToken));

        [HttpPost("{orderId:long}/Cancellations")]
        public async Task<IActionResult> Cancel(
            long orderId,
            [FromBody] CancelOrderRequest request,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeCancelOrderCommand(orderId, request.ServiceIds, request.Reason, request.ReasonDetail), cancellationToken));

        [HttpPost("{orderId:long}/Documents/Voids")]
        public async Task<IActionResult> VoidDocuments(
            long orderId,
            [FromBody] VoidDocumentsRequest request,
            CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeVoidElectronicTicketsCommand(orderId, request.Tickets, request.Reason, request.ReasonDetail), cancellationToken));
    }
}
