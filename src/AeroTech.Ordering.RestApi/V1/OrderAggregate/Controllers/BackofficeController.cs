
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Backoffice")]
    [Route($"Backoffice/v{{version:apiVersion}}/Orders")]
    public sealed class BackofficeController : ControllerBase
    {
        private readonly IMediator _mediator;      

        public BackofficeController(IMediator mediator)
        {
            _mediator = mediator;
            
        }

    }
}
