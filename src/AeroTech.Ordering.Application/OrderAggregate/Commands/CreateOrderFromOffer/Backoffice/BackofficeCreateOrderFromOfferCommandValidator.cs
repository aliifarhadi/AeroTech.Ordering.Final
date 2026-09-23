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

            RuleForEach(command => command.Travelers).SetValidator(new OrderTravellerValidator());

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
