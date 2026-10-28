using FluentValidation;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.Backoffice
{
    public sealed class BackofficeAddAncillariesCommandValidator : AddAncillariesValidator<BackofficeAddAncillariesCommand>
    {
        public BackofficeAddAncillariesCommandValidator()
        {
            RuleFor(command => command.SelectedAncillaries).NotNull();
            RuleFor(command => command.SelectedAncillaries.SelectionId).NotEmpty().MaximumLength(SelectionIdMaxLength);
        }
    }
}
