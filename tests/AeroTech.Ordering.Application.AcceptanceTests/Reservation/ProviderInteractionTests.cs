using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Providers.FlightFlow.Wire;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ProviderInteractionTests
{
    private const string OtherProviderKey = "ProviderB";

    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Exact_request_snapshot_is_durable_before_the_first_dispatch()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(request =>
        {
            var interaction = Assert.Single(Assert.Single(_harness.Tasks.Committed).Interactions);

            Assert.Equal(1, _harness.UnitOfWork.SaveCount);
            Assert.Equal(ProviderInteractionStatus.Pending, interaction.Status);
            Assert.Equal(Serialized(request), interaction.RequestPayload);
            Assert.Equal(HashOf(interaction.RequestPayload), interaction.RequestHash);

            return _harness.FlightFlow.Held(request);
        });

        await _harness.ReserveOrderAsync(order);

        Assert.Single(_harness.FlightFlow.HoldRequests);
    }

    [Fact]
    public async Task Unknown_retry_replays_the_original_request_even_after_order_facts_change()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));

        var first = await _harness.ReserveOrderAsync(order);
        ChangeGender(order.Travellers.Single(), Gender.Female);
        await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(first.Reservations.Single().ReservationId);
        var interactions = InteractionsOf(reservation);

        Assert.NotEqual(interactions[0].RequestPayload, RebuiltRequest(order, reservation).Payload);
        Assert.Equal(Serialized(_harness.FlightFlow.HoldRequests[0]), Serialized(_harness.FlightFlow.HoldRequests[1]));
        Assert.All(interactions, interaction =>
        {
            Assert.Equal(Serialized(_harness.FlightFlow.HoldRequests[0]), interaction.RequestPayload);
            Assert.Equal(interactions[0].RequestHash, interaction.RequestHash);
            Assert.Equal(reservation.IdempotencyKey, interaction.IdempotencyKey);
            Assert.Equal(reservation.CorrelationReference, interaction.CorrelationReference);
        });
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Every_provider_call_is_its_own_interaction_linked_to_its_attempt()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome, "Timed out."));

        var first = await _harness.ReserveOrderAsync(order);
        await _harness.ReserveOrderAsync(order);

        var task = _harness.TaskOf(first.Reservations.Single().ReservationId, OrderFulfillmentTaskType.ReserveInventory);
        var attempts = task.Attempts.OrderBy(attempt => attempt.AttemptNumber).ToList();

        Assert.Equal(
            [
                (attempts[0].Id, 1, 1, ProviderInteractionType.CreateHold, ProviderInteractionStatus.TimedOut),
                (attempts[1].Id, 2, 1, ProviderInteractionType.CreateHold, ProviderInteractionStatus.Succeeded)
            ],
            InteractionsOf(task).Select(interaction => (
                interaction.FulfillmentTaskAttemptId,
                interaction.AttemptNumber,
                interaction.Sequence,
                interaction.InteractionType,
                interaction.Status)));
        Assert.All(task.Interactions, interaction => Assert.Equal(FulfillmentProviderKeys.FlightFlow, interaction.FulfillmentProviderKey));
    }

    [Fact]
    public async Task One_attempt_holds_an_inconclusive_read_back_and_the_mutation_replay()
    {
        var provider = new StubReservationProvider(OtherProviderKey, ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held, supportsReadBack: true);
        var harness = new ReservationHarness(provider);
        var order = SeedOrderOf(harness);
        provider.ReserveResponses.Enqueue(provider.Unknown);
        provider.ReadResponses.Enqueue(provider.Unknown);

        var first = await harness.ReserveOrderAsync(order);
        await harness.ReserveOrderAsync(order);

        var reservation = harness.Reservation(first.Reservations.Single().ReservationId);
        var task = harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory);
        var secondAttempt = task.Attempts.Single(attempt => attempt.AttemptNumber == 2);

        Assert.Equal(
            [
                (1, ProviderInteractionType.ReadReservation, reservation.ProviderOperationRef, ProviderInteractionStatus.TimedOut),
                (2, ProviderInteractionType.CreateHold, InteractionsOf(task)[0].RequestPayload, ProviderInteractionStatus.Succeeded)
            ],
            task.Interactions
                .Where(interaction => interaction.FulfillmentTaskAttemptId == secondAttempt.Id)
                .OrderBy(interaction => interaction.Sequence)
                .Select(interaction => (interaction.Sequence, interaction.InteractionType, (string?)interaction.RequestPayload, interaction.Status)));
        Assert.Equal(2, provider.ReserveCalls.Count);
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Conclusive_read_back_settles_the_reservation_without_replaying_the_mutation()
    {
        var provider = new StubReservationProvider(OtherProviderKey, ReservationMode.HoldThenConfirm, ReservationMemberStatus.Held, supportsReadBack: true);
        var harness = new ReservationHarness(provider);
        var order = SeedOrderOf(harness);
        provider.ReserveResponses.Enqueue(provider.Unknown);
        provider.ReadResponses.Enqueue(provider.Resolved);

        var first = await harness.ReserveOrderAsync(order);
        await harness.ReserveOrderAsync(order);

        var reservation = harness.Reservation(first.Reservations.Single().ReservationId);
        var task = harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory);

        Assert.Single(provider.ReserveCalls);
        Assert.Equal(
            [(1, ProviderInteractionType.CreateHold), (2, ProviderInteractionType.ReadReservation)],
            InteractionsOf(task).Select(interaction => (interaction.AttemptNumber, interaction.InteractionType)));
        Assert.Equal(FulfillmentReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public async Task Successful_response_evidence_is_preserved()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        var result = await _harness.ReserveOrderAsync(order);

        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        var interaction = Assert.Single(InteractionsOf(reservation));
        var expectedBody = ScriptedFlightFlowProvider.BodyOf(_harness.FlightFlow.Held(_harness.FlightFlow.HoldRequests.Single()));

        Assert.Equal(
            (ProviderInteractionStatus.Succeeded, (int?)201, reservation.ProviderOperationRef, expectedBody, HashOf(expectedBody), (string?)null),
            (interaction.Status, interaction.ProviderStatusCode, interaction.ProviderOperationRef, interaction.ResponsePayload, interaction.ResponseHash, interaction.Error));
        Assert.NotNull(interaction.CompletedAt);
    }

    [Fact]
    public async Task Rejected_response_evidence_is_preserved()
    {
        const string body = """{"errors":[{"code":4001,"title":"No seats."}]}""";
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        _harness.FlightFlow.HoldResponses.Enqueue(_ => throw new ProviderRequestException(
            FulfillmentFailureKind.Permanent, FulfillmentFailureReason.BusinessRejected, "No seats.", 400, body));

        var result = await _harness.ReserveOrderAsync(order);

        var interaction = Assert.Single(InteractionsOf(_harness.Reservation(result.Reservations.Single().ReservationId)));

        Assert.Equal(
            (ProviderInteractionStatus.Failed, (int?)400, "No seats.", body, HashOf(body)),
            (interaction.Status, interaction.ProviderStatusCode, interaction.Error, interaction.ResponsePayload, interaction.ResponseHash));
    }

    [Fact]
    public async Task Normalized_business_result_lives_on_the_reservation_not_in_the_interaction_log()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var result = await _harness.ReserveOrderAsync(order);
        var reservation = _harness.Reservation(result.Reservations.Single().ReservationId);
        var interaction = Assert.Single(InteractionsOf(reservation));
        var held = _harness.FlightFlow.Held(_harness.FlightFlow.HoldRequests.Single());

        typeof(ProviderInteraction).GetProperty(nameof(ProviderInteraction.ResponsePayload))!.SetValue(interaction, null);
        var repeated = await _harness.ReserveOrderAsync(order);

        Assert.Empty(repeated.Reservations);
        Assert.Equal(
            (FulfillmentReservationStatus.Held, held.HoldId, (DateTimeOffset?)held.ExpiresAt),
            (reservation.Status, reservation.ProviderOperationRef, reservation.ExpiresAt));
        Assert.Equal(
            held.Seats.Select(seat => seat.SeatHoldReference),
            reservation.Units.Select(unit => unit.ProviderUnitRef));
        Assert.Equal(OrderStatus.Confirmed, repeated.Status);
    }

    private static Order SeedOrderOf(ReservationHarness harness)
    {
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        ReservationHarness.AssignProvider(order, 1, OtherProviderKey);
        return order;
    }

    private List<ProviderInteraction> InteractionsOf(FulfillmentReservation reservation)
        => InteractionsOf(_harness.TaskOf(reservation.Id, OrderFulfillmentTaskType.ReserveInventory));

    private static List<ProviderInteraction> InteractionsOf(FulfillmentTask task)
        => task.Interactions
            .OrderBy(interaction => interaction.AttemptNumber)
            .ThenBy(interaction => interaction.Sequence)
            .ToList();

    private ProviderRequest RebuiltRequest(Order order, FulfillmentReservation reservation)
        => _harness.FlightFlowReservation.ReserveRequestFor(
            order,
            new ReservationIntent(
                reservation.FulfillmentProviderKey,
                reservation.IdempotencyKey,
                reservation.CorrelationReference,
                reservation.RequestedExpiresAt,
                _harness.FlightFlowReservation.PlanUnits(order, ReservationHarness.AirServices(order))));

    private static void ChangeGender(OrderTraveller traveller, Gender gender)
    {
        var current = traveller.ProfileRevisions.Single(revision => revision.Id == traveller.CurrentProfileRevisionId);
        typeof(TravellerProfileRevision).GetProperty(nameof(TravellerProfileRevision.Gender))!.SetValue(current, gender);
    }

    private static string Serialized(HoldSeatsRequest request) => JsonSerializer.Serialize(request, FlightFlowJson.Options);

    private static string HashOf(string payload) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
}
