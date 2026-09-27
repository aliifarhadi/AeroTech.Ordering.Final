using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain._Shared.Contracts;
using MediatR;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets.Backoffice
{
    public sealed class BackofficeVoidElectronicTicketsCommandHandler : IRequestHandler<BackofficeVoidElectronicTicketsCommand, VoidElectronicTicketsResult>
    {
        private readonly IVoidElectronicTicketsService _service;
        private readonly ICallerContext _callerContext;

        public BackofficeVoidElectronicTicketsCommandHandler(IVoidElectronicTicketsService service, ICallerContext callerContext)
        {
            _service = service;
            _callerContext = callerContext;
        }

        public Task<VoidElectronicTicketsResult> Handle(BackofficeVoidElectronicTicketsCommand command, CancellationToken cancellationToken)
            => _service.VoidAsync(
                command.OrderId,
                command.Tickets,
                command.Reason,
                command.ReasonDetail,
                UnrestrictedOrderAuthorization.Instance,
                _callerContext.ActorId,
                _callerContext.AirlineOfficeId,
                cancellationToken);
    }
}
