using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.IssueOrder
{
    public abstract class IssueOrderValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IIssueOrderCommand
    {
        protected IssueOrderValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.TicketDocumentStockId).GreaterThan(0);
        }
    }
}
