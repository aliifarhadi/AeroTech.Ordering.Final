using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application._Shared.Caller;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class FixedSalesContextFactory(SalesContext salesContext) : ISalesContextFactory
{
    public SalesContext Create(SalesChannel channel) => salesContext;
}
