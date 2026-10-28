using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Application._Shared.Caller;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.Ota
{
    public sealed class OtaAddAncillariesCommandHandler : IRequestHandler<OtaAddAncillariesCommand, AddAncillariesResult>
    {
        private const SalesChannel Channel = SalesChannel.PartnerAPI;

        private readonly IAddAncillariesService _service;
        private readonly ISalesContextFactory _salesContextFactory;
        private readonly CallerOwnedOrderAuthorization _authorization;

        public OtaAddAncillariesCommandHandler(
            IAddAncillariesService service,
            ISalesContextFactory salesContextFactory,
            CallerOwnedOrderAuthorization authorization)
        {
            _service = service;
            _salesContextFactory = salesContextFactory;
            _authorization = authorization;
        }

        public Task<AddAncillariesResult> Handle(OtaAddAncillariesCommand command, CancellationToken cancellationToken)
            => _service.AddAsync(
                command.OrderId,
                command.ExpectedCommercialVersion,
                command.Services.Single().Id,
                _authorization,
                _salesContextFactory.Create(Channel),
                cancellationToken);
    }
}
