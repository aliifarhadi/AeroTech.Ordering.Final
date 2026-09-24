using FluentValidation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public abstract class ReleaseReservationValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IReleaseReservationCommand
    {
        protected ReleaseReservationValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.ReservationId).GreaterThan(0);
        }
    }
}
