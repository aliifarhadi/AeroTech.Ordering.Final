using System.Security.Claims;
using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.ServiceHost.CallerContext
{
    public sealed class ClaimsCallerContext : ICallerContext
    {
        public const string ContextTypeClaim = "context_type";
        public const string PrincipalTypeClaim = "principal_type";
        public const string AirlineUserIdClaim = "airline_user_id";
        public const string TravelAgencyUserIdClaim = "travel_agency_user_id";
        public const string TravelAgencyIdClaim = "travel_agency_id";
        public const string TravelAgencyOfficeIdClaim = "travel_agency_office_id";
        public const string IndividualIdClaim = "individual_id";
        public const string PartnerApiAccessProfileIdClaim = "partner_api_access_profile_id";
        public const string CustomerIdClaim = "customer_id";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsCallerContext(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        private bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public CallerContextType ContextType =>
            !IsAuthenticated
                ? CallerContextType.None
                : Enum.TryParse<CallerContextType>(Read(ContextTypeClaim), ignoreCase: true, out var value)
                    ? value
                    : CallerContextType.None;

        public CallerPrincipalType PrincipalType =>
            !IsAuthenticated
                ? CallerPrincipalType.None
                : Enum.TryParse<CallerPrincipalType>(Read(PrincipalTypeClaim), ignoreCase: true, out var value)
                    ? value
                    : CallerPrincipalType.None;

        public long? AirlineUserId => ReadId(AirlineUserIdClaim);

        public long? TravelAgencyUserId => ReadId(TravelAgencyUserIdClaim);

        public long? TravelAgencyId => ReadId(TravelAgencyIdClaim);

        public IReadOnlyCollection<long> TravelAgencyOfficeIds =>
            !IsAuthenticated
                ? []
                : Principal!.FindAll(TravelAgencyOfficeIdClaim)
                    .Select(claim => long.TryParse(claim.Value, out var id) ? id : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToArray();

        public long? IndividualId => ReadId(IndividualIdClaim);

        public long? PartnerApiAccessProfileId => ReadId(PartnerApiAccessProfileIdClaim);

        public long? CustomerId => ReadId(CustomerIdClaim);

        public long ActorId =>
            AirlineUserId
            ?? TravelAgencyUserId
            ?? IndividualId
            ?? PartnerApiAccessProfileId
            ?? 0;

        private string? Read(string claimType) =>
            IsAuthenticated ? Principal!.FindFirst(claimType)?.Value : null;

        private long? ReadId(string claimType) =>
            long.TryParse(Read(claimType), out var value) ? value : null;
    }
}
