using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Backoffice
{
    public sealed class BackofficeCreateOrderFromOfferCommandValidator
        : AbstractValidator<BackofficeCreateOrderFromOfferCommand>
    {
        public BackofficeCreateOrderFromOfferCommandValidator()
        {
            RuleFor(command => command.OfferId).NotEmpty();
            RuleFor(command => command.CustomerId).GreaterThan(0);
            RuleFor(command => command.Contact).NotNull();
            RuleFor(command => command.Travelers).NotEmpty();
            RuleFor(command => command.AncillaryPricedSelectionId).NotEmpty().When(command => command.AncillaryPricedSelectionId is not null);
            RuleFor(command => command.SeatPricedSelectionId).NotEmpty().When(command => command.SeatPricedSelectionId is not null);

            RuleForEach(command => command.Travelers).SetValidator(new OrderTravellerValidator());

            RuleForEach(command => command.Remarks).ChildRules(remark =>
                remark.RuleFor(item => item.Text).NotEmpty());
        }
    }
}
