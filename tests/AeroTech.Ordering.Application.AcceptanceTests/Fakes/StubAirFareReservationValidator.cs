using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.Pricing;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubAirFareReservationValidator(FixedClock clock) : IAirFareReservationValidator
{
    public List<IReadOnlyCollection<long>> Calls { get; } = [];

    public Func<Order, IReadOnlyCollection<long>, DateTimeOffset>? Behavior { get; set; }

    public Task<ReservationValidationEvidence> ValidateAsync(
        Order order,
        IReadOnlyCollection<long> airServiceIds,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(airServiceIds.ToList());

        var validUntil = Behavior?.Invoke(order, airServiceIds) ?? clock.Now.AddDays(1);
        var validatedServiceIds = order.FarePricingAtomsCovering(airServiceIds)
            .SelectMany(atom => atom.AirServiceIds)
            .Union(airServiceIds)
            .Order()
            .ToList();

        return Task.FromResult(new ReservationValidationEvidence(order.CommercialVersion, validUntil, clock.Now, validatedServiceIds));
    }
}
