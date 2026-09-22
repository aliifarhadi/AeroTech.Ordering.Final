namespace AeroTech.Ordering.ReferenceData.ReadModels
{
    public sealed class TravelAgencyReadModel : IReferenceReadModel<long>
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
}
