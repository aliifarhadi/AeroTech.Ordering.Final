namespace AeroTech.Ordering.Domain._Shared.Contracts
{
    public enum CallerContextType
    {
        None = 0,
        Airline = 1,
        TravelAgency = 2,
        Individual = 3,
        PartnerApi = 4,
        Service = 5
    }

    public enum CallerPrincipalType
    {
        None = 0,
        Human = 1,
        TravelAgencyApi = 2,
        InternalService = 3,
        PlatformAutomation = 4
    }

    public interface ICallerContext
    {
        CallerContextType ContextType { get; }

        CallerPrincipalType PrincipalType { get; }

        long? AirlineUserId { get; }

        long? AirlineOfficeId { get; }

        long? TravelAgencyUserId { get; }

        long? TravelAgencyId { get; }

        IReadOnlyCollection<long> TravelAgencyOfficeIds { get; }

        long? IndividualId { get; }

        long? PartnerApiAccessProfileId { get; }

        long? CustomerId { get; }

        long ActorId { get; }
    }
}
