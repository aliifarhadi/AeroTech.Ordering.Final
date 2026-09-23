namespace AeroTech.Ordering.ReferenceData.ReadModels
{
    public sealed class CountryReadModel : IReferenceReadModel<int>
    {
        public int Id { get; set; }
        public string Alpha2Code { get; set; } = default!;
        public string? Alpha3Code { get; set; }
        public int NumericCode { get; set; }
        public string DisplayName { get; set; } = default!;
        public string? PhoneCode { get; set; }
        public DateTimeOffset LastUpdateTime { get; set; }
    }
}
