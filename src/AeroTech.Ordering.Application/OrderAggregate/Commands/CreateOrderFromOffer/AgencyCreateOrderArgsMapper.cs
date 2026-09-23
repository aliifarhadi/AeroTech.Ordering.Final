using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Application._Shared.Caller;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed class AgencyCreateOrderArgsMapper
    {
        private readonly ISalesContextFactory _salesContextFactory;
        private readonly ICallerCustomer _callerCustomer;
        private readonly OrderInputMapper _input;

        public AgencyCreateOrderArgsMapper(
            ISalesContextFactory salesContextFactory,
            ICallerCustomer callerCustomer,
            OrderInputMapper input)
        {
            _salesContextFactory = salesContextFactory;
            _callerCustomer = callerCustomer;
            _input = input;
        }

        public async Task<CreateOrderArgs> ToArgsAsync(
            IAgencyCreateOrderCommand command,
            SalesChannel channel,
            CancellationToken cancellationToken)
            => new(
                command.OfferId,
                _callerCustomer.RequiredId(),
                _salesContextFactory.Create(channel),
                OrderInputMapper.ToContactArgs(command.Contact),
                await _input.ToTravellerArgsAsync(command.Travelers, cancellationToken),
                OrderInputMapper.ToSeatArgs(command.SeatSelections),
                OrderInputMapper.ToRemarkArgs(command.Remarks));
    }
}
