namespace AeroTech.Ordering.ReferenceData.ReadModels
{
    public sealed class AirlineOfficeReadModel : IReferenceReadModel<long>
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
}
