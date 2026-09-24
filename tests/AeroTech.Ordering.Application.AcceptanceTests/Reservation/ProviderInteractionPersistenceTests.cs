using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Persistence.FulfillmentTaskAggregate;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ProviderInteractionPersistenceTests : IAsyncLifetime
{
    private const long ReservationId = 2;

    private readonly FixedClock _clock = new();
    private readonly SequentialIdGenerator _ids = new();
    private readonly TestDatabase _database;

    public ProviderInteractionPersistenceTests() => _database = new TestDatabase(_clock);

    public async Task InitializeAsync()
    {
        await using var context = _database.NewContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Interaction_evidence_survives_persistence_and_reload()
    {
        var task = FulfillmentTask.Create(
            _ids.NewId(),
            1,
            ReservationId,
            OrderFulfillmentTaskType.ReserveInventory,
            FulfillmentProviderKeys.FlightFlow,
            "reserve-hold:2",
            "order:1:reservation:2",
            [3],
            OrderFulfillmentTargetAction.Reserve,
            _ids,
            _clock.Now);

        task.StartAttempt(_ids, _clock.Now);
        task.RecordRequest(new ProviderRequest(ProviderInteractionType.CreateHold, "reserve-hold:2", "order:1:reservation:2", Payload('q')), _ids, _clock.Now);
        task.RecordResponse(ProviderOperationOutcome.Succeeded, "HOLD-1", null, new ProviderResponse(201, Payload('r')), _clock.Now);
        task.CompleteAttempt(FulfillmentAttemptOutcome.Succeeded, null, _clock.Now);

        await using (var context = _database.NewContext())
        {
            await new FulfillmentTaskRepository(context).AddAsync(task);
            await context.SaveChangesAsync();
        }

        await using (var context = _database.NewContext())
        {
            var reloaded = await new FulfillmentTaskRepository(context).FindLatestAsync(ReservationId, OrderFulfillmentTaskType.ReserveInventory);

            Assert.NotNull(reloaded);
            Assert.Equal(Evidence(Assert.Single(task.Interactions)), Evidence(Assert.Single(reloaded.Interactions)));
        }
    }

    private static string Payload(char filler) => $"{{\"body\":\"{new string(filler, 5000)}\"}}";

    private static string Evidence(ProviderInteraction interaction)
        => string.Join(
            "|",
            interaction.Id,
            interaction.FulfillmentTaskId,
            interaction.FulfillmentTaskAttemptId,
            interaction.AttemptNumber,
            interaction.Sequence,
            interaction.FulfillmentProviderKey,
            interaction.InteractionType,
            interaction.IdempotencyKey,
            interaction.CorrelationReference,
            interaction.RequestPayload,
            interaction.RequestHash,
            interaction.ResponsePayload,
            interaction.ResponseHash,
            interaction.ProviderOperationRef,
            interaction.Status,
            interaction.StartedAt,
            interaction.CompletedAt,
            interaction.ProviderStatusCode,
            interaction.Error);
}
