using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries
{
    public sealed record AddAncillariesResult(
        long OrderId,
        long OrderChangeId,
        int CommercialVersion,
        int CurrencyId,
        decimal CustomerTotal,
        IReadOnlyList<AddedAncillaryService> Services);

    public sealed record AddedAncillaryService(long OrderServiceId, OrderServiceType ServiceType, string VariantCode);
}
