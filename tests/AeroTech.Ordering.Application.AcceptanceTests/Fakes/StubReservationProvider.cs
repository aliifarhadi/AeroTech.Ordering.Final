using System.Globalization;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers.Reservation;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubReservationProvider(
    string providerKey,
    ReservationMode mode,
    ReservationMemberStatus result,
    bool supportsReadBack = false,
    bool expiresAutomatically = false,
    bool supportsSafeConfirmReplay = false)
    : IReservationProvider
{
    public List<ReservationIntent> ReserveCalls { get; } = [];

    public List<ProviderRequest> ReadCalls { get; } = [];

    public List<ReleaseIntent> ReleaseCalls { get; } = [];

    public List<ProviderRequest> ConfirmCalls { get; } = [];

    public List<ProviderRequest> CancelConfirmedCalls { get; } = [];

    public Queue<Func<ProviderRequest, ConfirmedCancellationOutcome>> CancelConfirmedResponses { get; } = new();

    public Queue<Func<ReservationIntent, ReservationOutcome>> ReserveResponses { get; } = new();

    public Queue<Func<ReservationIntent, ReservationOutcome>> ReadResponses { get; } = new();

    public Queue<Func<ProviderRequest, ConfirmationOutcome>> ConfirmResponses { get; } = new();

    public DateTimeOffset? ValidationTimeLimit { get; set; }

    public string ProviderKey => providerKey;

    public ReservationCapability CapabilityFor(OrderService service)
        => new(mode, BatchResultMode.PerUnit, ReservationActionScope.Unit, ReservationActionScope.Unit, false, false, true, supportsReadBack, expiresAutomatically, supportsSafeConfirmReplay);

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
        => Task.FromResult(new ReservationPreparation(null, ValidationTimeLimit));

    public ProviderRequest ReserveRequestFor(Order order, ReservationIntent intent)
        => new(
            ProviderInteractionType.CreateHold,
            intent.IdempotencyKey,
            intent.CorrelationReference,
            string.Join(",", intent.Units.Select(unit => unit.UnitCorrelationKey)));

    public Task<ReservationOutcome> ReserveAsync(ReservationIntent intent, ProviderRequest request, CancellationToken cancellationToken = default)
    {
        ReserveCalls.Add(intent);

        var respond = ReserveResponses.TryDequeue(out var scripted) ? scripted : Resolved;
        return Task.FromResult(respond(intent));
    }

    public ProviderRequest ReadRequestFor(string providerOperationRef)
        => new(ProviderInteractionType.ReadReservation, null, null, providerOperationRef);

    public Task<ReservationOutcome> ReadAsync(ReservationIntent intent, ProviderRequest request, CancellationToken cancellationToken = default)
    {
        ReadCalls.Add(request);
        return Task.FromResult(ReadResponses.Dequeue()(intent));
    }

    public ProviderRequest ReleaseRequestFor(ReleaseIntent intent)
        => new(ProviderInteractionType.ReleaseHold, intent.IdempotencyKey, null, intent.ProviderOperationRef);

    public Task<ReleaseOutcome> ReleaseAsync(ProviderRequest request, CancellationToken cancellationToken = default)
    {
        ReleaseCalls.Add(new ReleaseIntent(providerKey, request.Payload, request.IdempotencyKey!));
        return Task.FromResult(new ReleaseOutcome(ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Released, null, null));
    }

    public ProviderRequest ConfirmRequestFor(ConfirmationIntent intent)
        => new(ProviderInteractionType.ConfirmHold, null, null, intent.ProviderOperationRef);

    public Task<ConfirmationOutcome> ConfirmAsync(ProviderRequest request, CancellationToken cancellationToken = default)
    {
        ConfirmCalls.Add(request);

        var respond = ConfirmResponses.TryDequeue(out var scripted) ? scripted : Confirmed;
        return Task.FromResult(respond(request));
    }

    public ProviderRequest CancelConfirmedRequestFor(ConfirmedCancellationIntent intent)
        => new(
            ProviderInteractionType.CancelConfirmed,
            null,
            null,
            $"{intent.ProviderOperationRef}:{string.Join(",", intent.Units.Select(unit => unit.ProviderUnitRef))}");

    public Task<ConfirmedCancellationOutcome> CancelConfirmedAsync(ProviderRequest request, CancellationToken cancellationToken = default)
    {
        CancelConfirmedCalls.Add(request);

        var respond = CancelConfirmedResponses.TryDequeue(out var scripted) ? scripted : CancelledConfirmed;
        return Task.FromResult(respond(request));
    }

    public static ConfirmedCancellationOutcome CancelledConfirmed(ProviderRequest request)
        => new(ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Cancelled, null, null);

    public static ConfirmedCancellationOutcome UnobservedCancellation(ProviderRequest request)
        => new(
            ProviderOperationOutcome.Unknown,
            null,
            new ProviderFailure(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The cancellation outcome was not observed.", null),
            null);

    public ReservationOutcome Resolved(ReservationIntent intent)
        => result == ReservationMemberStatus.Rejected
            ? new ReservationOutcome(ProviderOperationOutcome.Rejected, null, null, null, null, [], null, null)
            : new ReservationOutcome(
                ProviderOperationOutcome.Succeeded,
                OperationRefOf(intent),
                intent.IdempotencyKey,
                intent.CorrelationReference,
                null,
                intent.Units
                    .Select(unit => new ReservationUnitOutcome(unit.UnitCorrelationKey, unit.OrderServiceIds, result, $"{providerKey}-{unit.UnitCorrelationKey}", null, null))
                    .ToList(),
                null,
                null);

    public ReservationOutcome Unknown(ReservationIntent intent)
        => new(
            ProviderOperationOutcome.Unknown,
            OperationRefOf(intent),
            null,
            null,
            null,
            [],
            new ProviderFailure(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The outcome was not observed.", null),
            null);

    public static ConfirmationOutcome Confirmed(ProviderRequest request)
        => new(ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Confirmed, null, null);

    public static ConfirmationOutcome RejectedConfirmation(ProviderRequest request)
        => new(
            ProviderOperationOutcome.Rejected,
            null,
            new ProviderFailure(FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "The provider refused to confirm.", null),
            null);

    public static ConfirmationOutcome UnobservedConfirmation(ProviderRequest request)
        => new(
            ProviderOperationOutcome.Unknown,
            null,
            new ProviderFailure(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "The confirmation outcome was not observed.", null),
            null);

    public string OperationRefOf(ReservationIntent intent) => $"{providerKey}-{intent.IdempotencyKey}";

    private sealed record StubUnitDetails(long OrderServiceId) : ReservationUnitDetails;
}
