using FluentValidation;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public abstract class VoidElectronicTicketsValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IVoidElectronicTicketsCommand
    {
        protected VoidElectronicTicketsValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.Tickets).NotEmpty();
            RuleFor(command => command.Tickets)
                .Must(tickets => tickets.Select(ticket => ticket.ElectronicTicketId).Distinct().Count() == tickets.Count)
                .When(command => command.Tickets is not null)
                .WithMessage("Each electronic ticket may be requested only once.");
            RuleForEach(command => command.Tickets).ChildRules(ticket =>
            {
                ticket.RuleFor(target => target.ElectronicTicketId).GreaterThan(0);
                ticket.RuleFor(target => target.ExpectedDocumentVersion).GreaterThan(0);
            });
            RuleFor(command => command.Reason).IsInEnum();
            RuleFor(command => command.ReasonDetail).MaximumLength(500);
        }
    }
}
