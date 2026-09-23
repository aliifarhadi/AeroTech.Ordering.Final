using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application._Shared.Authorization
{
    public sealed class CallerCustomer : ICallerCustomer
    {
        private readonly ICallerContext _callerContext;

        public CallerCustomer(ICallerContext callerContext) => _callerContext = callerContext;

        public long RequiredId()
            => _callerContext.CustomerId ?? throw ExceptionFactory.CallerHasNoCustomerContext();
    }
}
