using AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderAncillaryContext.Service;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Service2Service")]
    [Route($"Service/v{{version:apiVersion}}/Orders")]
    public sealed class ServiceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ServiceController(IMediator mediator) => _mediator = mediator;

        [HttpGet("{orderId:long}/AncillaryContext")]
        public async Task<IActionResult> GetAncillaryContext(long orderId, CancellationToken cancellationToken)
            => Ok(await _mediator.Send(new ServiceGetOrderAncillaryContextQuery(orderId), cancellationToken));
    }
}
