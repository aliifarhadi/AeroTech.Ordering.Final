using System.Security.Claims;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.ServiceHost.CallerContext
{
    public sealed class CallerContextActorResolver : IActorResolver
    {
        private readonly ICallerContext _callerContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CallerContextActorResolver(ICallerContext callerContext, IHttpContextAccessor httpContextAccessor)
        {
            _callerContext = callerContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public Actor Resolve()
        {
            var subject =
                _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return new Actor(subject, _callerContext.ActorId, _callerContext.ContextType.ToString());
        }
    }
}
