using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Arguments
{
    public sealed record OrderAncillaryServiceArgs(
        long Id,
        long OrderId,
        long OrderItemId,
        long? TravellerId,
        AncillaryVariant Variant,
        int Quantity,
        SelectionScope SelectionScope,
        ServiceCoverageScope CoverageScope,
        IReadOnlyCollection<long> CoveredAirServiceIds,
        AncillarySaleSource Source,
        AncillaryDefinition Definition,
        AncillaryProvision Provision,
        AncillaryPricing Pricing,
        long CreatedByChangeId,
        DateTimeOffset CreatedAt);
}
