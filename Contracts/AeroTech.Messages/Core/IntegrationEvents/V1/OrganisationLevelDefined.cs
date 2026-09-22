using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record OrganisationLevelDefined(
        long OrganisationLevelId,
        string Code,
        string Name,
        LifecycleStatus Status,
        DateOnly? ValidFrom,
        DateOnly? ValidTo,
        long SourceVersion) : BaseIntegrationEvent;
}
