namespace AeroTech.Ordering.ReferenceData.ReadModels
{
    public sealed class CustomerReadModel : IReferenceReadModel<long>
    {
        public long Id { get; set; }
        public string CustomerNumber { get; set; } = default!;
        public CustomerType Type { get; set; }
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
}
