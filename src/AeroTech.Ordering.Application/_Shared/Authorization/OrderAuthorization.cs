using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application._Shared.Authorization
{
    public sealed class CallerOwnedOrderAuthorization : IOrderAuthorization
    {
        private readonly ICallerCustomer _callerCustomer;

        public CallerOwnedOrderAuthorization(ICallerCustomer callerCustomer) => _callerCustomer = callerCustomer;

        public void Ensure(Order order)
        {
            if (order.CustomerId != _callerCustomer.RequiredId())
                throw ExceptionFactory.OrderDoesNotBelongToCaller(order.Id);
        }
    }

    public sealed class UnrestrictedOrderAuthorization : IOrderAuthorization
    {
        public static readonly UnrestrictedOrderAuthorization Instance = new();

        public void Ensure(Order order)
        {
        }
    }
}
