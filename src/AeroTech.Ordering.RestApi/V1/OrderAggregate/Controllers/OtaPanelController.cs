
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate
{
    [ApiController]
    [ApiVersion("1.0")]
    [Tags("OTA Panel")]
    [Route($"OtaPanel/v{{version:apiVersion}}/Bookings")]
    public sealed class OtaPanelController : ControllerBase
    {
        private readonly IMediator _mediator;


        public OtaPanelController(IMediator mediator)
        {
            _mediator = mediator;
           
        }

    }
}
