using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class FlightFlowAtomicSemanticsTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public void Held_seats_lapse_automatically_at_their_expiry_and_cannot_be_read_back()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var capability = _harness.FlightFlowReservation.CapabilityFor(ReservationHarness.AirService(order, 1, 101));

        Assert.Equal((true, false), (capability.ExpiresAutomatically, capability.SupportsReadBack));
    }

    [Fact]
    public async Task Success_holds_every_expected_unit()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101, 102)]);

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Held, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Succeeded, _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory).Status);
    }

    [Fact]
    public async Task Permanent_failure_rejects_every_target_unit()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Flight is closed for sale.", 422));

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory);

        Assert.Equal(FulfillmentReservationStatus.Rejected, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Rejected, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Failed, task.Status);
        Assert.Equal(ProviderInteractionStatus.Failed, task.Interactions.Single().Status);
        Assert.Equal(422, task.Interactions.Single().ProviderStatusCode);
        Assert.Equal(FulfillmentFailureReason.ProviderRejected, result.Reservations.Single().FailureReason);
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);
        Assert.Null(order.RecordLocator);
    }

    [Theory]
    [InlineData(FulfillmentFailureKind.Retriable, FulfillmentFailureReason.TechnicalFailed, 503, ProviderInteractionStatus.Failed)]
    [InlineData(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, null, ProviderInteractionStatus.TimedOut)]
    public async Task Retriable_or_indeterminate_failure_is_unknown(
        FulfillmentFailureKind kind,
        FulfillmentFailureReason reason,
        int? statusCode,
        ProviderInteractionStatus interactionStatus)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(kind, reason, "FlightFlow did not answer.", statusCode));

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        var task = _harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory);

        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.All(reservation.Units, unit => Assert.Equal(ReservationMemberStatus.Unknown, unit.Status));
        Assert.Equal(OrderFulfillmentStatus.Unknown, task.Status);
        Assert.Equal(interactionStatus, task.Interactions.Single().Status);
        Assert.Equal(OrderStatus.ReservationUnconfirmed, order.Status);
    }

    [Fact]
    public async Task Incomplete_success_payload_is_an_unknown_protocol_inconsistency()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request =>
        {
            var held = _harness.FlightFlow.Held(request);
            return held with { Seats = held.Seats.Take(1).ToList() };
        });

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.Equal($"HOLD-{reservation.IdempotencyKey}", reservation.ProviderOperationRef);
        Assert.Equal(FulfillmentFailureReason.UnknownOutcome, result.Reservations.Single().FailureReason);
    }

    public static TheoryData<string> MalformedResponses => ["missing seat", "foreign idempotency key", "foreign reference", "duplicate seat", "missing seat status"];

    [Theory]
    [MemberData(nameof(MalformedResponses))]
    public async Task Malformed_success_payload_is_unknown(string malformation)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request => Malformed(_harness.FlightFlow.Held(request), malformation));

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        Assert.Equal(FulfillmentReservationStatus.Unknown, reservation.Status);
        Assert.DoesNotContain(reservation.Units, unit => unit.Status == ReservationMemberStatus.Held);
    }

    public static TheoryData<string> AdapterResponses => ["success", "missing seat", "foreign idempotency key", "foreign reference", "duplicate seat", "missing seat status", "permanent", "retriable", "indeterminate"];

    [Theory]
    [MemberData(nameof(AdapterResponses))]
    public async Task Adapter_never_emits_partial(string response)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1), TravellerSpec.Adult(2)], [new BoundSpec("OUT", 101, 102)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request => response switch
        {
            "success" => _harness.FlightFlow.Held(request),
            "permanent" => throw new ProviderRequestException(FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected, "Rejected.", 422),
            "retriable" => throw new ProviderRequestException(FulfillmentFailureKind.Retriable, FulfillmentFailureReason.TechnicalFailed, "Unavailable.", 503),
            "indeterminate" => throw new ProviderRequestException(FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."),
            _ => Malformed(_harness.FlightFlow.Held(request), response)
        });

        var units = _harness.FlightFlowReservation.PlanUnits(order, order.Services);
        var intent = new ReservationIntent(
            FulfillmentProviderKeys.FlightFlow,
            "reserve-hold:1",
            "order:1:reservation:1",
            _harness.Clock.Now.AddHours(1),
            units);

        var outcome = await _harness.FlightFlowReservation.ReserveAsync(intent, _harness.FlightFlowReservation.ReserveRequestFor(order, intent));

        Assert.NotEqual(ProviderOperationOutcome.Partial, outcome.OperationOutcome);
        Assert.Contains(outcome.OperationOutcome, new[] { ProviderOperationOutcome.Succeeded, ProviderOperationOutcome.Rejected, ProviderOperationOutcome.Unknown });
    }

    [Fact]
    public async Task Replayed_hold_reports_the_current_provider_status()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request => _harness.FlightFlow.WithStatus(request, FlightSeatHoldStatus.Expired));

        var result = await _harness.ReserveOrderAsync(order);

        Assert.Equal(FulfillmentReservationStatus.Expired, result.Reservations.Single().Status);
        Assert.Equal(OrderStatus.ReserveFailed, order.Status);
    }

    private static FlightHeldSeatsResult Malformed(FlightHeldSeatsResult held, string malformation) => malformation switch
    {
        "missing seat" => held with { Seats = held.Seats.Skip(1).ToList() },
        "foreign idempotency key" => held with { IdempotencyKey = "reserve-hold:other" },
        "foreign reference" => held with { Reference = "order:other" },
        "duplicate seat" => held with { Seats = [held.Seats[0], held.Seats[0]] },
        "missing seat status" => held with { Seats = held.Seats.Select(seat => seat with { SeatHoldStatus = null }).ToList() },
        _ => throw new ArgumentOutOfRangeException(nameof(malformation))
    };
}
