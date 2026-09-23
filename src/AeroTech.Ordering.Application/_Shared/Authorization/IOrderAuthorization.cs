using AeroTech.Ordering.Domain.OrderAggregate;

namespace AeroTech.Ordering.Application._Shared.Authorization
{
    public interface IOrderAuthorization
    {
        void Ensure(Order order);
    }
}
