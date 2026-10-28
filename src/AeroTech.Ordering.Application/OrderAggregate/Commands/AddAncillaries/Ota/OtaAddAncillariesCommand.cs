using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries.Ota
{
    public sealed record OtaAddAncillariesCommand(
        long OrderId,
        int ExpectedCommercialVersion,
        IReadOnlyList<AncillaryServiceReference> Services) : IRequest<AddAncillariesResult>, IAgencyAddAncillariesCommand;
}
