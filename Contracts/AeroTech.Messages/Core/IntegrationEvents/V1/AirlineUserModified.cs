using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record AirlineUserModified(
        long AirlineUserId,
        string DisplayName,
        long? DefaultAirlineOfficeId,
        BusinessUserStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
