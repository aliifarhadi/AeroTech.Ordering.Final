using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.Backoffice
{
    public sealed record BackofficeAddAncillariesCommand(
        long OrderId,
        int ExpectedCommercialVersion,
        SelectedAncillariesInput SelectedAncillaries) : IRequest<AddAncillariesResult>, IAddAncillariesCommand;
}
