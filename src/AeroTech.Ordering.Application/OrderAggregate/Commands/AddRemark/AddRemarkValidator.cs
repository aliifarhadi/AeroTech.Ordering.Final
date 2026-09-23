using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public abstract class AddRemarkValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IAddRemarkCommand
    {
        protected AddRemarkValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.Remark).NotNull();
            RuleFor(command => command.Remark.Text).NotEmpty();
            RuleFor(command => command.SupersedesRemarkId).GreaterThan(0).When(command => command.SupersedesRemarkId.HasValue);
        }
    }
}
