using AeroTech.Ordering.ReferenceData.ReadModels;
using AeroTech.Ordering.ReferenceData.Syncing;

namespace AeroTech.Ordering.ReferenceData.Core.Wire
{
    public sealed class CoreEnvelope<T>
    {
        public T? Data { get; set; }

        public CoreError[]? Errors { get; set; }
    }

    public sealed class CoreError
    {
        public int? Code { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }
    }

    public sealed class CustomerDto : ISyncSourceDto<long>
    {
        public long Id { get; set; }
        public string CustomerNumber { get; set; } = default!;
        public CustomerType CustomerType { get; set; }
        public long? IndividualId { get; set; }
        public long? TravelAgencyId { get; set; }
        public long? OrganizationId { get; set; }
        public long SubjectId { get; set; }
        public string SubjectName { get; set; } = default!;
        public CustomerStatus Status { get; set; }
        public DateOnly RelationshipStartedOn { get; set; }
        public DateOnly? RelationshipEndedOn { get; set; }
        public int? PreferredCurrencyId { get; set; }
        public string? PreferredLanguageCode { get; set; }
        public string? SuspensionReasonCode { get; set; }
        public string? ClosureReasonCode { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }

    public sealed class OperatorSettingsDto : ISyncSourceDto<long>
    {
        public long Id { get; set; }
        public string ScopeKey { get; set; } = default!;
        public long HomeAirlineId { get; set; }
        public int? DefaultCurrencyId { get; set; }
        public string? DefaultLanguageCode { get; set; }
        public string? DefaultTimeZoneId { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }

    public sealed class AirlineOfficeDto : ISyncSourceDto<long>
    {
        public long Id { get; set; }
        public long? OrganisationUnitId { get; set; }
        public string? OrganisationUnitName { get; set; }
        public long? ParentOfficeId { get; set; }
        public string? ParentOfficeName { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public long LegalEntityId { get; set; }
        public string LegalEntityLegalName { get; set; } = default!;
        public DateOnly LegalEntityEffectiveFrom { get; set; }
        public int CountryId { get; set; }
        public int? CityId { get; set; }
        public int? AirportId { get; set; }
        public int? PointOfSaleCountryId { get; set; }
        public int? PointOfSaleCityId { get; set; }
        public string? TimeZoneId { get; set; }
        public OfficeStatus Status { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }

    public sealed class TravelAgencyDto : ISyncSourceDto<long>
    {
        public long Id { get; set; }
        public string Code { get; set; } = default!;
        public string LegalName { get; set; } = default!;
        public string? TradingName { get; set; }
        public long? ParentAgencyId { get; set; }
        public string? ParentAgencyLegalName { get; set; }
        public int CountryId { get; set; }
        public int? PreferredCurrencyId { get; set; }
        public string? PreferredLanguageCode { get; set; }
        public AgencyStatus Status { get; set; }
        public DateTimeOffset? OnboardedAt { get; set; }
        public DateTimeOffset? TerminatedAt { get; set; }
        public string? TerminationReasonCode { get; set; }
        public string? PrimaryAccreditationTypeCode { get; set; }
        public string? PrimaryAccreditationIdentifier { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }

    public sealed class TravelAgencyOfficeDto : ISyncSourceDto<long>
    {
        public long Id { get; set; }
        public long TravelAgencyId { get; set; }
        public string TravelAgencyLegalName { get; set; } = default!;
        public long? ParentOfficeId { get; set; }
        public string? ParentOfficeName { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int CountryId { get; set; }
        public int? CityId { get; set; }
        public int? AirportId { get; set; }
        public int? PointOfSaleCountryId { get; set; }
        public int? PointOfSaleCityId { get; set; }
        public string? TimeZoneId { get; set; }
        public OfficeStatus Status { get; set; }
        public DateOnly? ValidFrom { get; set; }
        public DateOnly? ValidTo { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }
}
