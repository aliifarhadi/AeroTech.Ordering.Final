using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed class OrderTravellerValidator : AbstractValidator<OrderTraveller>
    {
        public OrderTravellerValidator()
        {
            RuleFor(traveller => traveller.Id).GreaterThan(0);
            RuleFor(traveller => traveller.Name).NotNull();
            RuleFor(traveller => traveller.Name.FirstName).NotEmpty();
            RuleFor(traveller => traveller.Name.LastName).NotEmpty().When(traveller => !traveller.Name.NoLastName);
            RuleFor(traveller => traveller.Nationality).Length(2).When(traveller => traveller.Nationality is not null);
            RuleFor(traveller => traveller.CountryOfResidence).Length(2).When(traveller => traveller.CountryOfResidence is not null);

            RuleForEach(traveller => traveller.Documents).ChildRules(document =>
            {
                document.RuleFor(item => item.Number).NotEmpty();
                document.RuleFor(item => item.IssuanceCountry).NotEmpty().Length(2);
            });
        }
    }
}
