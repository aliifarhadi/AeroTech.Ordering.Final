using System.Security.Claims;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.ServiceHost.CallerContext
{
    public sealed class ClaimsCallerContext : ICallerContext
    {
        public const string ContextTypeClaim = "context_type";
        public const string PrincipalTypeClaim = "principal_type";
        public const string AirlineUserIdClaim = "airline_user_id";
        public const string AirlineOfficeIdClaim = "airline_office_id";
        public const string TravelAgencyUserIdClaim = "travel_agency_user_id";
        public const string TravelAgencyIdClaim = "travel_agency_id";
        public const string TravelAgencyOfficeIdClaim = "travel_agency_office_id";
        public const string IndividualIdClaim = "individual_id";
        public const string PartnerApiAccessProfileIdClaim = "partner_api_access_profile_id";
        public const string CustomerIdClaim = "customer_id";

        private static readonly IReadOnlyDictionary<string, CallerContextType> ContextTypes =
            new Dictionary<string, CallerContextType>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(CallerContextType.Airline)] = CallerContextType.Airline,
                [nameof(CallerContextType.TravelAgency)] = CallerContextType.TravelAgency,
                [nameof(CallerContextType.Individual)] = CallerContextType.Individual,
                [nameof(CallerContextType.PartnerApi)] = CallerContextType.PartnerApi,
                [nameof(CallerContextType.Service)] = CallerContextType.Service
            };

        private static readonly IReadOnlyDictionary<string, CallerPrincipalType> PrincipalTypes =
            new Dictionary<string, CallerPrincipalType>(StringComparer.Ordinal)
            {
                ["human"] = CallerPrincipalType.Human,
                ["travel_agency_api"] = CallerPrincipalType.TravelAgencyApi,
                ["internal_service"] = CallerPrincipalType.InternalService,
                ["platform_automation"] = CallerPrincipalType.PlatformAutomation
            };

        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsCallerContext(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        private bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public CallerContextType ContextType
        {
            get
            {
                if (!IsAuthenticated)
                    return CallerContextType.None;

                var claim = Read(ContextTypeClaim);

                return claim is not null && ContextTypes.TryGetValue(claim, out var value)
                    ? value
                    : throw ExceptionFactory.CallerContextTypeIsNotRecognised(claim);
            }
        }

        public CallerPrincipalType PrincipalType
        {
            get
            {
                if (!IsAuthenticated)
                    return CallerPrincipalType.None;

                var claim = Read(PrincipalTypeClaim);

                return claim is not null && PrincipalTypes.TryGetValue(claim, out var value)
                    ? value
                    : throw ExceptionFactory.CallerPrincipalTypeIsNotRecognised(claim);
            }
        }

        public long? AirlineUserId => ReadId(AirlineUserIdClaim);

        public long? AirlineOfficeId => ReadId(AirlineOfficeIdClaim);

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
