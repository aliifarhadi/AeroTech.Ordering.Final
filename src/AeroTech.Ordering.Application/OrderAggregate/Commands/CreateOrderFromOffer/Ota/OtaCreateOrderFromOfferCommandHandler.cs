using AeroTech.Messages.Shared.Enums;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Ota
{
    public sealed class OtaCreateOrderFromOfferCommandHandler
        : IRequestHandler<OtaCreateOrderFromOfferCommand, CreateOrderFromOfferResult>
    {
        private const SalesChannel Channel = SalesChannel.PartnerAPI;

        private readonly ICreateOrderFromOfferService _service;
        private readonly AgencyCreateOrderArgsMapper _mapper;

        public OtaCreateOrderFromOfferCommandHandler(
            ICreateOrderFromOfferService service,
            AgencyCreateOrderArgsMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        public async Task<CreateOrderFromOfferResult> Handle(
            OtaCreateOrderFromOfferCommand command,
            CancellationToken cancellationToken)
            => await _service.ExecuteAsync(
                await _mapper.ToArgsAsync(command, Channel, cancellationToken),
                cancellationToken);
    }
}
