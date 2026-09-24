using AeroTech.Messages.FlightFlow.Enums;
using AeroTech.Ordering.Domain.Providers.FlightFlow;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class ScriptedFlightFlowProvider(Func<string, string> flightIdOfCapacity, InMemoryUnitOfWork unitOfWork)
    : IFlightFlowProvider
{
    public List<HoldSeatsRequest> HoldRequests { get; } = [];

    public List<int> SaveCountAtHold { get; } = [];

    public List<ReleaseHeldSeatsRequest> ReleaseRequests { get; } = [];

    public Queue<Func<HoldSeatsRequest, FlightHeldSeatsResult>> HoldResponses { get; } = new();

    public Queue<Func<ReleaseHeldSeatsRequest, ReleaseHeldSeatsResult>> ReleaseResponses { get; } = new();

    public Task<FlightHeldSeatsResult> CreateHoldAsync(HoldSeatsRequest request, CancellationToken cancellationToken = default)
    {
        HoldRequests.Add(request);
        SaveCountAtHold.Add(unitOfWork.SaveCount);

        var respond = HoldResponses.TryDequeue(out var scripted) ? scripted : Held;
        return Task.FromResult(respond(request));
    }

    public Task<ReleaseHeldSeatsResult> ReleaseHeldAsync(ReleaseHeldSeatsRequest request, CancellationToken cancellationToken = default)
    {
        ReleaseRequests.Add(request);

        var respond = ReleaseResponses.TryDequeue(out var scripted) ? scripted : _ => new ReleaseHeldSeatsResult(true, null);
        return Task.FromResult(respond(request));
    }

    public FlightHeldSeatsResult Held(HoldSeatsRequest request)
        => WithStatus(request, FlightSeatHoldStatus.Held);

    public FlightHeldSeatsResult WithStatus(HoldSeatsRequest request, FlightSeatHoldStatus status)
        => new(
            $"HOLD-{request.IdempotencyKey}",
            request.IdempotencyKey,
            request.Reference,
            request.ExpiresAt,
            Seats(request)
                .Select(seat => new HeldSeat(seat.FlightId, seat.PaxReference, seat.Seat, status, $"SHR-{seat.FlightId}-{seat.PaxReference}"))
                .ToList());

    public IReadOnlyList<(string FlightId, string PaxReference, string? Seat)> Seats(HoldSeatsRequest request)
        => request.Flights
            .SelectMany(flight => flight.Seats.Select(seat => (flightIdOfCapacity(flight.FlightCapId), seat.PaxReference, seat.Seat)))
            .ToList();

    public Task ConfirmHoldAsync(ConfirmHoldRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<CancelConfirmedSeatsResult> CancelConfirmedAsync(CancelConfirmedSeatsRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SplitHeldSeatsResult> SplitHeldAsync(SplitHeldSeatsRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task ExtendHeldAsync(ExtendHeldSeatsRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
