using FluentValidation;

namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public abstract class ReserveServicesValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IReserveServicesCommand
    {
        protected ReserveServicesValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.OrderServiceIds).NotEmpty();
            RuleForEach(command => command.OrderServiceIds).GreaterThan(0);
        }
    }
}
