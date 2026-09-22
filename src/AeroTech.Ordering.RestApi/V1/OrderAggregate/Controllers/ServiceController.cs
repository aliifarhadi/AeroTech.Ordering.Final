using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("Service2Service")]
    [Route($"Service/v{{version:apiVersion}}/Bookings")]
    public sealed class ServiceController : ControllerBase
    {
            private readonly IMediator _mediator;
       

        public ServiceController(IMediator mediator)
        {
            _mediator = mediator;
            
        } 

    }
}
