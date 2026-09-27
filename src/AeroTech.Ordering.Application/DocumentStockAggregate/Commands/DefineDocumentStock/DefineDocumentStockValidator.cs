using FluentValidation;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock
{
    public abstract class DefineDocumentStockValidator<TCommand> : AbstractValidator<TCommand>
        where TCommand : IDefineDocumentStockCommand
    {
        protected DefineDocumentStockValidator()
        {
            RuleFor(command => command.OwnerAirlineId).GreaterThan(0);
            RuleFor(command => command.OfficeId).GreaterThan(0).When(command => command.OfficeId is not null);
            RuleFor(command => command.DocumentKind).IsInEnum();
            RuleFor(command => command.Prefix).NotEmpty().MaximumLength(32);
            RuleFor(command => command.SerialWidth).GreaterThan(0);
            RuleFor(command => command.CheckDigitProfile).NotEmpty().MaximumLength(32);
            RuleFor(command => command.RangeFrom).GreaterThan(0);
            RuleFor(command => command.RangeTo).GreaterThanOrEqualTo(command => command.RangeFrom);
        }
    }
}
