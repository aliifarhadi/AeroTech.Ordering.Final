using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record IndividualModified(
        long IndividualId,
        string DisplayName,
        LifecycleStatus Status,
        long SourceVersion,
        string? IdentitySubjectId = null) : BaseIntegrationEvent;
}
