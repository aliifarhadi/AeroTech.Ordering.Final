using System.Globalization;
using System.Security.Claims;
using AeroTech.Framework.Core.ServiceContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AeroTech.Framework.Presentation.AspNetCore.Services
{
    public sealed class ClaimsActorResolver : IActorResolver
    {
        public const string ActorIdClaim = "actor_id";
        public const string ActorTypeClaim = "actor_type";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsActorResolver(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        public Actor Resolve()
        {
            var principal = _httpContextAccessor.HttpContext?.User;

            if (principal?.Identity?.IsAuthenticated != true)
                return Actor.Anonymous;

            var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                          ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return new Actor(
                Subject: subject,
                ActorId: ReadActorId(principal),
                ActorType: principal.FindFirstValue(ActorTypeClaim));
        }

        private static long? ReadActorId(ClaimsPrincipal principal)
        {
            var raw = principal.FindFirstValue(ActorIdClaim);

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actorId)
                ? actorId
                : null;
        }
    }
}
