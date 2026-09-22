namespace AeroTech.Ordering.ReferenceData.ReadModels
{
    public sealed class TravelAgencyOfficeReadModel : IReferenceReadModel<long>
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
