using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.OtaPanel
{
    public sealed record OtaPanelAddAncillariesCommand(
        long OrderId,
        int ExpectedCommercialVersion,
        IReadOnlyList<AncillaryServiceReference> Services) : IRequest<AddAncillariesResult>, IAgencyAddAncillariesCommand;
}
