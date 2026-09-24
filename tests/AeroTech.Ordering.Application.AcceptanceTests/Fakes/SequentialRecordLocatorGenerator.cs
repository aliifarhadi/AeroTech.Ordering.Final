using AeroTech.Ordering.Domain.OrderAggregate.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class SequentialRecordLocatorGenerator : IRecordLocatorGenerator
{
    public List<string> Generated { get; } = [];

    public string Generate()
    {
        var recordLocator = $"PNR{Generated.Count + 1:D3}";
        Generated.Add(recordLocator);
        return recordLocator;
    }
}
