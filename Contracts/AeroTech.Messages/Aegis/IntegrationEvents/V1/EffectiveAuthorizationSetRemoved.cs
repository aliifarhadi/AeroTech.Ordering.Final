using AeroTech.Messages.Aegis.Enums;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Messages.Aegis.IntegrationEvents.V1
{
    public sealed record EffectiveAuthorizationSetRemoved(
        long EffectiveAuthorizationSetId,
        GranteeType GranteeType,
        string GranteeKey,
        long? AirlineUserId,
        long? TravelAgencyUserId,
        long? IndividualId,
        string? SubjectId,
        long? PartnerApiAccessProfileId,
        string? ServiceCode,
        AuthorizationSurface AuthorizationSurface,
        string ContextKey,
        string Reason,
        DateTimeOffset RemovedAt) : BaseIntegrationEvent;
}
