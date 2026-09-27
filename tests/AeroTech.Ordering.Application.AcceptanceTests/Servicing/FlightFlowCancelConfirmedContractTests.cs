using System.Net;
using System.Text.Json;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class FlightFlowCancelConfirmedContractTests
{
    private const string HoldBatchId = "HOLD-1";
    private const string CancelledStatus = "5";
    private const string ConfirmedStatus = "4";

    public static TheoryData<HttpStatusCode, string, ProviderOperationOutcome, FulfillmentReservationStatus?> Responses => new()
    {
        { HttpStatusCode.OK, Body(HoldBatchId, ("501", CancelledStatus), ("502", CancelledStatus)), ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Cancelled },
        { HttpStatusCode.OK, Body(HoldBatchId, ("502", CancelledStatus), ("501", CancelledStatus)), ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Cancelled },
        { HttpStatusCode.OK, Body(HoldBatchId, ("501", CancelledStatus), ("502", ConfirmedStatus)), ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.OK, Body(HoldBatchId, ("501", CancelledStatus)), ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.OK, Body(HoldBatchId, ("501", CancelledStatus), ("502", CancelledStatus), ("503", CancelledStatus)), ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.OK, Body("HOLD-2", ("501", CancelledStatus), ("502", CancelledStatus)), ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.OK, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.NoContent, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.Created, Body(HoldBatchId, ("501", CancelledStatus), ("502", CancelledStatus)), ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.Accepted, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1186), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Unknown },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1187), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Held },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1188), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Released },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1189), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Expired },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1190), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Mixed },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1166), ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.Forbidden, string.Empty, ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.NotFound, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.InternalServerError, string.Empty, ProviderOperationOutcome.Unknown, null }
    };

    [Fact]
    public async Task Cancellation_posts_the_exact_selected_references_with_the_native_reason_and_no_idempotency_identity()
    {
        var handler = new StubHttpMessageHandler(_ => FlightFlowWire.Response(HttpStatusCode.OK, Body(HoldBatchId, ("501", CancelledStatus), ("502", CancelledStatus))));
        var provider = ProviderOver(handler);

        var request = provider.CancelConfirmedRequestFor(Intent());
        await provider.CancelConfirmedAsync(request);

        var (sent, body) = handler.Requests.Single();
        using var payload = JsonDocument.Parse(body);
        Assert.Equal((HttpMethod.Post, $"https://provider.test/service/v1/Flights/Seat-Confirmations/{HoldBatchId}/Cancellation"), (sent.Method, sent.RequestUri!.ToString()));
        Assert.Equal(2, payload.RootElement.GetProperty("reasonCode").GetInt32());
        Assert.Equal(["501", "502"], payload.RootElement.GetProperty("seatHoldReferences").EnumerateArray().Select(reference => reference.GetString()));
        Assert.Equal((ProviderInteractionType.CancelConfirmed, (string?)null, (string?)null), (request.InteractionType, request.IdempotencyKey, request.CorrelationReference));
    }

    [Theory]
    [MemberData(nameof(Responses))]
    public async Task Cancellation_response_maps_to_a_provider_neutral_outcome(
        HttpStatusCode status,
        string body,
        ProviderOperationOutcome expectedOutcome,
        FulfillmentReservationStatus? expectedStatus)
    {
        var handler = new StubHttpMessageHandler(_ => FlightFlowWire.Response(status, body));

        var outcome = await CancelAsync(handler);

        Assert.Equal((expectedOutcome, expectedStatus), (outcome.OperationOutcome, outcome.ObservedStatus));
        Assert.Equal<int?>((int)status, outcome.Response!.StatusCode);
    }

    [Fact]
    public async Task Timed_out_cancellation_is_an_unknown_outcome()
    {
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("The request timed out."));

        var outcome = await CancelAsync(handler);

        Assert.Equal((ProviderOperationOutcome.Unknown, FulfillmentFailureReason.UnknownOutcome), (outcome.OperationOutcome, outcome.Failure!.Reason));
    }

    [Fact]
    public async Task Connection_failure_during_cancellation_is_an_unknown_outcome()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("The connection was reset."));

        var outcome = await CancelAsync(handler);

        Assert.Equal(ProviderOperationOutcome.Unknown, outcome.OperationOutcome);
    }

    private static ConfirmedCancellationIntent Intent()
        => new(FulfillmentProviderKeys.FlightFlow, HoldBatchId, [new ConfirmedCancellationUnit(11, "501"), new ConfirmedCancellationUnit(12, "502")], nameof(VoidReason.CustomerRequest));

    private static Task<ConfirmedCancellationOutcome> CancelAsync(StubHttpMessageHandler handler)
    {
        var provider = ProviderOver(handler);

        return provider.CancelConfirmedAsync(provider.CancelConfirmedRequestFor(Intent()));
    }

    private static string Body(string holdBatchId, params (string Reference, string Status)[] seats)
        => $$"""{"data":{"holdBatchId":"{{holdBatchId}}","seats":[{{string.Join(",", seats.Select(seat => $$"""{"seatHoldReference":"{{seat.Reference}}","status":{{seat.Status}}}"""))}}]},"errors":null}""";

    private static FlightFlowReservationProvider ProviderOver(StubHttpMessageHandler handler)
        => new(new FlightFlowProvider(StubHttpMessageHandler.ClientFor(handler)), new StubAirFareReservationValidator(new FixedClock()));
}
