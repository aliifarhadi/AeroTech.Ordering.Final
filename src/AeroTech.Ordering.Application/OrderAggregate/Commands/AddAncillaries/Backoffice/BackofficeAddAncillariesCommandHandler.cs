using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Application._Shared.Caller;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.Backoffice
{
    public sealed class BackofficeAddAncillariesCommandHandler : IRequestHandler<BackofficeAddAncillariesCommand, AddAncillariesResult>
    {
        private const SalesChannel Channel = SalesChannel.BackOffice;

        private readonly IAddAncillariesService _service;
        private readonly ISalesContextFactory _salesContextFactory;

        public BackofficeAddAncillariesCommandHandler(IAddAncillariesService service, ISalesContextFactory salesContextFactory)
        {
            _service = service;
            _salesContextFactory = salesContextFactory;
        }

        public Task<AddAncillariesResult> Handle(BackofficeAddAncillariesCommand command, CancellationToken cancellationToken)
            => _service.AddAsync(
                command.OrderId,
                command.ExpectedCommercialVersion,
                command.SelectedAncillaries.SelectionId,
                UnrestrictedOrderAuthorization.Instance,
                _salesContextFactory.Create(Channel),
                cancellationToken);
    }
}
