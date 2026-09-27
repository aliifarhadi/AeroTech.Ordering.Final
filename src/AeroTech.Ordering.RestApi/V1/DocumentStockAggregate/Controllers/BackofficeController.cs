using AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock.Backoffice;
using AeroTech.Ordering.Query.DocumentStockAggregate.Queries.GetDocumentStockById.Backoffice;
using AeroTech.Ordering.RestApi.V1.DocumentStockAggregate.Requests;
using AeroTech.Ordering.RestApi.V1._Shared;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.DocumentStockAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Backoffice")]
    [Route($"Backoffice/v{{version:apiVersion}}/DocumentStocks")]
    [Authorize(SurfaceAuthorization.Backoffice)]
    public sealed class BackofficeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BackofficeController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        public async Task<IActionResult> Define([FromBody] DefineDocumentStockRequest request, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new BackofficeDefineDocumentStockCommand(
                request.OwnerAirlineId,
                request.OfficeId,
                request.DocumentKind,
                request.Prefix,
                request.SerialWidth,
                request.CheckDigitProfile,
                request.RangeFrom,
                request.RangeTo), cancellationToken));

        [HttpGet("{documentStockId:long}")]
        public async Task<IActionResult> GetById(long documentStockId, CancellationToken cancellationToken)
        {
            var stock = await _mediator.Send(new BackofficeGetDocumentStockByIdQuery(documentStockId), cancellationToken);

            return stock is null ? NotFound() : Ok(stock);
        }
    }
}
