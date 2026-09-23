namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed record CreateOrderFromOfferResult(
        long OrderId,
        Guid OrderReference,
        int CurrencyId,
        decimal CustomerTotal,
        int CommercialVersion);
}
