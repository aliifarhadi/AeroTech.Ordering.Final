using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record IndividualDefined(
        long IndividualId,
        string DisplayName,
        LifecycleStatus Status,
        long SourceVersion,
        string? IdentitySubjectId = null) : BaseIntegrationEvent;
}
