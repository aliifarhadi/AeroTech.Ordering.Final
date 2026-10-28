using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries
{
    public abstract class AddAncillariesValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IAddAncillariesCommand
    {
        protected const int SelectionIdMaxLength = 1024;

        protected AddAncillariesValidator()
        {
            RuleFor(command => command.OrderId).GreaterThan(0);
            RuleFor(command => command.ExpectedCommercialVersion).GreaterThan(0);
        }
    }

    public abstract class AgencyAddAncillariesValidator<TCommand> : AddAncillariesValidator<TCommand>
        where TCommand : IAgencyAddAncillariesCommand
    {
        protected AgencyAddAncillariesValidator()
        {
            RuleFor(command => command.Services).NotNull().Must(services => services.Count == 1);
            RuleForEach(command => command.Services).ChildRules(service =>
            {
                service.RuleFor(reference => reference.Type).Equal(AncillaryServiceReference.AncillarySelection);
                service.RuleFor(reference => reference.Id).NotEmpty().MaximumLength(SelectionIdMaxLength);
            });
        }
    }
}
