using System.Globalization;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubReservationProvider(string providerKey, ReservationMode mode, ReservationMemberStatus result)
    : IReservationProvider
{
    public List<ReservationIntent> ReserveCalls { get; } = [];

    public List<ReleaseIntent> ReleaseCalls { get; } = [];

    public string ProviderKey => providerKey;

    public ReservationCapability CapabilityFor(OrderService service)
        => new(mode, BatchResultMode.PerUnit, ReservationActionScope.Unit, ReservationActionScope.Unit, false, false, true);

    public IReadOnlyList<ReservationUnitIntent> PlanUnits(Order order, IReadOnlyCollection<OrderService> services)
        => services
            .Select(service => new ReservationUnitIntent(
                service.Id.ToString(CultureInfo.InvariantCulture),
                [service.Id],
                new StubUnitDetails(service.Id)))
            .ToList();

    public Task<ReservationPreparation> PrepareAsync(
        Order order,
        IReadOnlyList<ReservationUnitIntent> units,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ReservationPreparation(null));

    public Task<ReservationOutcome> ReserveAsync(Order order, ReservationIntent intent, CancellationToken cancellationToken = default)
    {
        ReserveCalls.Add(intent);

        return Task.FromResult(result == ReservationMemberStatus.Rejected
            ? new ReservationOutcome(ProviderOperationOutcome.Rejected, null, null, null, null, [], null)
            : new ReservationOutcome(
                ProviderOperationOutcome.Succeeded,
                $"{providerKey}-{intent.IdempotencyKey}",
                intent.IdempotencyKey,
                intent.CorrelationReference,
                null,
                intent.Units
                    .Select(unit => new ReservationUnitOutcome(unit.UnitCorrelationKey, unit.OrderServiceIds, result, $"{providerKey}-{unit.UnitCorrelationKey}", null, null))
                    .ToList(),
                null));
    }

    public Task<ReleaseOutcome> ReleaseAsync(ReleaseIntent intent, CancellationToken cancellationToken = default)
    {
        ReleaseCalls.Add(intent);
        return Task.FromResult(new ReleaseOutcome(ProviderOperationOutcome.Succeeded, null));
    }

    private sealed record StubUnitDetails(long OrderServiceId) : ReservationUnitDetails;
}
