using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record TravelAgencyOfficeStatusChanged(
        long TravelAgencyOfficeId,
        long TravelAgencyId,
        OfficeStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
