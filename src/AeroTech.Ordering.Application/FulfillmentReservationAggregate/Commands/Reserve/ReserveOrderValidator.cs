using FluentValidation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public abstract class ReserveOrderValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IReserveOrderCommand
    {
        protected ReserveOrderValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
        }
    }
}
