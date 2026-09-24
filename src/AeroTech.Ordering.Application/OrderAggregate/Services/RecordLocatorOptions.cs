namespace AeroTech.Ordering.Application.OrderAggregate.Services
{
    public sealed class RecordLocatorOptions
    {
        public const string SectionName = "RecordLocator";

        public int MaxAllocationAttempts { get; set; }
    }
}
