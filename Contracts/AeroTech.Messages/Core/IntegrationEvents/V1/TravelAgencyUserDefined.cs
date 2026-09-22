using AeroTech.Messages.Core.Enums;

namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record TravelAgencyUserDefined(
        long TravelAgencyUserId,
        long TravelAgencyId,
        string DisplayName,
        long? DefaultTravelAgencyOfficeId,
        BusinessUserStatus Status,
        long SourceVersion) : BaseIntegrationEvent;
}
