using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application._Shared.Caller
{
    public interface ISalesContextFactory
    {
        SalesContext Create(SalesChannel channel);
    }
}
