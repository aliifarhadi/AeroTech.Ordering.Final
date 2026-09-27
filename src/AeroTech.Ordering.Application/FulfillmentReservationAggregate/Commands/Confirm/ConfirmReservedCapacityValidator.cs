using FluentValidation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public abstract class ConfirmReservedCapacityValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IConfirmReservedCapacityCommand
    {
        protected ConfirmReservedCapacityValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
        }
    }
}
