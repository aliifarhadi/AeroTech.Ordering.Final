using AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record BackofficeAddAncillariesRequest(int ExpectedCommercialVersion, SelectedAncillariesInput SelectedAncillaries);

    public sealed record AgencyAddAncillariesRequest(int ExpectedCommercialVersion, IReadOnlyList<AncillaryServiceReference> Services);
}
