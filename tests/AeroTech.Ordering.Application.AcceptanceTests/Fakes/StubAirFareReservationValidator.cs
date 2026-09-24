using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.Pricing;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubAirFareReservationValidator(FixedClock clock) : IAirFareReservationValidator
{
    public List<IReadOnlyCollection<long>> Calls { get; } = [];

    public Func<Order, IReadOnlyCollection<long>, DateTimeOffset>? Behavior { get; set; }

    public Task<DateTimeOffset> ValidateAsync(
        Order order,
        IReadOnlyCollection<long> airServiceIds,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(airServiceIds.ToList());
        return Task.FromResult(Behavior?.Invoke(order, airServiceIds) ?? clock.Now.AddDays(1));
    }
}
