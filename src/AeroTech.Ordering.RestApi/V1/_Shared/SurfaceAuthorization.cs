using System.Security.Claims;
using AeroTech.Messages.Shared;
using AeroTech.Messages.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.RestApi.V1._Shared
{
    public static class SurfaceAuthorization
    {
        public const string Backoffice = "Surface.Backoffice";
        public const string Ota = "Surface.Ota";
        public const string OtaPanel = "Surface.OtaPanel";

        private const string SurfaceClaim = "authz_surface";
        private const string ScopeClaim = "scope";

        public static IServiceCollection AddSurfaceAuthorization(this IServiceCollection services)
            => services.AddAuthorizationBuilder()
                .AddPolicy(Backoffice, policy => policy.RequireSurface(AuthorizationSurface.Backoffice))
                .AddPolicy(Ota, policy => policy.RequireSurface(AuthorizationSurface.Api))
                .AddPolicy(OtaPanel, policy => policy.RequireSurface(AuthorizationSurface.OtaPanel))
                .Services;

        private static void RequireSurface(this AuthorizationPolicyBuilder policy, AuthorizationSurface surface)
            => policy
                .RequireAuthenticatedUser()
                .RequireClaim(SurfaceClaim, surface.ToString().ToLowerInvariant())
                .RequireAssertion(context => HasScope(context.User, AuthorizationSurfaceScopes.Canonical(surface)));

        private static bool HasScope(ClaimsPrincipal user, string scope)
            => user.FindAll(ScopeClaim).Any(claim => claim.Value.Split(' ').Contains(scope));
    }
}
