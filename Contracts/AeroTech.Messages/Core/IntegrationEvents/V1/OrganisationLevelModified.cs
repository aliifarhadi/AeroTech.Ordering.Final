using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record OrganisationLevelModified(
        long OrganisationLevelId,
        string Code,
        string Name,
        LifecycleStatus Status,
        DateOnly? ValidFrom,
        DateOnly? ValidTo,
        long SourceVersion) : BaseIntegrationEvent;
}
