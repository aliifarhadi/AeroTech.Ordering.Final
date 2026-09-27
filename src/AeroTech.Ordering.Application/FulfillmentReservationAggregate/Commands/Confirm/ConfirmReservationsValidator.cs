using FluentValidation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public abstract class ConfirmReservationsValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IConfirmReservationsCommand
    {
        protected ConfirmReservationsValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.ReservationIds).NotEmpty();
            RuleForEach(command => command.ReservationIds).GreaterThan(0);
        }
    }
}
