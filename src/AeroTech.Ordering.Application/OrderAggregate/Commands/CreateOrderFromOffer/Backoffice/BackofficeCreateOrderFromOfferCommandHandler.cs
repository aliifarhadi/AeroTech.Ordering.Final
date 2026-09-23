using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Caller;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Backoffice
{
    public sealed class BackofficeCreateOrderFromOfferCommandHandler
        : IRequestHandler<BackofficeCreateOrderFromOfferCommand, CreateOrderFromOfferResult>
    {
        private const SalesChannel Channel = SalesChannel.BackOffice;

        private readonly ICreateOrderFromOfferService _service;
        private readonly ISalesContextFactory _salesContextFactory;
        private readonly OrderInputMapper _input;

        public BackofficeCreateOrderFromOfferCommandHandler(
            ICreateOrderFromOfferService service,
            ISalesContextFactory salesContextFactory,
            OrderInputMapper input)
        {
            _service = service;
            _salesContextFactory = salesContextFactory;
            _input = input;
        }

        public async Task<CreateOrderFromOfferResult> Handle(
            BackofficeCreateOrderFromOfferCommand command,
            CancellationToken cancellationToken)
        {
            var args = new CreateOrderArgs(
                command.OfferId,
                command.CustomerId,
                _salesContextFactory.Create(Channel),
                OrderInputMapper.ToContactArgs(command.Contact),
                await _input.ToTravellerArgsAsync(command.Travelers, cancellationToken),
                OrderInputMapper.ToSeatArgs(command.SeatSelections),
                OrderInputMapper.ToRemarkArgs(command.Remarks));

            return await _service.ExecuteAsync(args, cancellationToken);
        }
    }
}
