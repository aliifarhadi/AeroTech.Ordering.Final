using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record TravelAgencyModified(
        long TravelAgencyId,
        string Code,
        string Name,
        string? TradingName,
        AgencyStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
