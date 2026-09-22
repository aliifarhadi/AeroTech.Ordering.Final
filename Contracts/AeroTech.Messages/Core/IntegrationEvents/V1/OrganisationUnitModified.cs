using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record OrganisationUnitModified(
        long OrganisationUnitId,
        long? ParentOrganisationUnitId,
        string Code,
        string Name,
        LifecycleStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
