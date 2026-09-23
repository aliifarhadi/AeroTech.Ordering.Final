using AeroTech.Messages.Shared.Enums;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.OtaPanel
{
    public sealed class OtaPanelCreateOrderFromOfferCommandHandler
        : IRequestHandler<OtaPanelCreateOrderFromOfferCommand, CreateOrderFromOfferResult>
    {
        private const SalesChannel Channel = SalesChannel.AgencyPanel;

        private readonly ICreateOrderFromOfferService _service;
        private readonly AgencyCreateOrderArgsMapper _mapper;

        public OtaPanelCreateOrderFromOfferCommandHandler(
            ICreateOrderFromOfferService service,
            AgencyCreateOrderArgsMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        public async Task<CreateOrderFromOfferResult> Handle(
            OtaPanelCreateOrderFromOfferCommand command,
            CancellationToken cancellationToken)
            => await _service.ExecuteAsync(
                await _mapper.ToArgsAsync(command, Channel, cancellationToken),
                cancellationToken);
    }
}
