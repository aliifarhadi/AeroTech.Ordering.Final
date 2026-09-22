using AeroTech.Messages.Aegis.Enums;

namespace AeroTech.Messages.Aegis.IntegrationEvents.V1
{
    public sealed record PartnerApiAccessChanged(
        long PartnerApiAccessProfileId,
        long TravelAgencyId,
        PartnerApiAccessProfileStatus Status,
        long TravelAgencyOfficeId,
        long SourceVersion,
        DateTimeOffset ChangedAt) : BaseIntegrationEvent;
}
