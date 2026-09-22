using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record AirlineUserOrganisationMembershipChanged(
        long AirlineUserId,
        long MembershipId,
        long OrganisationUnitId,
        long? LevelId,
        bool IsPrimary,
        DateOnly? ValidFrom,
        DateOnly? ValidTo,
        LifecycleStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
