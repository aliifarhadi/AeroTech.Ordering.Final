using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public abstract class AgencyCreateOrderValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IAgencyCreateOrderCommand
    {
        protected AgencyCreateOrderValidator()
        {
            RuleFor(command => command.OfferId).NotEmpty();
            RuleFor(command => command.Contact).NotNull();
            RuleFor(command => command.Travelers).NotEmpty();

            RuleFor(command => command.Contact)
                .Must(contact => !string.IsNullOrWhiteSpace(contact.EmailAddress) || contact.Phones.Count > 0)
                .WithMessage("Contact requires an email address or at least one phone.");

            RuleForEach(command => command.Travelers).SetValidator(new OrderTravellerValidator());

            RuleForEach(command => command.Contact.Phones).ChildRules(phone =>
            {
                phone.RuleFor(item => item.CountryCallingCode).NotEmpty();
                phone.RuleFor(item => item.Number).NotEmpty();
            });

            RuleForEach(command => command.SeatSelections).ChildRules(seat =>
            {
                seat.RuleFor(item => item.BoundId).NotEmpty();
                seat.RuleFor(item => item.SeatNumber).NotEmpty();
            });

            RuleForEach(command => command.Remarks).ChildRules(remark =>
                remark.RuleFor(item => item.Text).NotEmpty());
        }
    }
}
