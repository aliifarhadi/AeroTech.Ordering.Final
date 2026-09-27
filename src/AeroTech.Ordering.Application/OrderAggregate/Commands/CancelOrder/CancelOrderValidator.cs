using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CancelOrder
{
    public abstract class CancelOrderValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : ICancelOrderCommand
    {
        protected CancelOrderValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleForEach(command => command.ServiceIds).GreaterThan(0);
            RuleFor(command => command.Reason).IsInEnum();
            RuleFor(command => command.ReasonDetail).MaximumLength(500);
        }
    }
}
