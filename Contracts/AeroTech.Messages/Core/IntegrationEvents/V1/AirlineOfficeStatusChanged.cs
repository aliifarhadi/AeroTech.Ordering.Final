using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record AirlineOfficeStatusChanged(
        long AirlineOfficeId,
        OfficeStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
