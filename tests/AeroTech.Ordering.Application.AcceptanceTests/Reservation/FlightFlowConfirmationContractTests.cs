using System.Net;
using System.Text;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class FlightFlowConfirmationContractTests
{
    private const string HoldId = "HOLD-1";

    public static TheoryData<HttpStatusCode, string, ProviderOperationOutcome, ReservationMemberStatus?> Responses => new()
    {
        { HttpStatusCode.NoContent, string.Empty, ProviderOperationOutcome.Succeeded, ReservationMemberStatus.Confirmed },
        { HttpStatusCode.BadRequest, ErrorBody(1176), ProviderOperationOutcome.Rejected, ReservationMemberStatus.Expired },
        { HttpStatusCode.BadRequest, ErrorBody(1177), ProviderOperationOutcome.Rejected, ReservationMemberStatus.Released },
        { HttpStatusCode.BadRequest, ErrorBody(1178), ProviderOperationOutcome.Rejected, ReservationMemberStatus.Cancelled },
        { HttpStatusCode.BadRequest, ErrorBody(1179), ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.NotFound, string.Empty, ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.InternalServerError, string.Empty, ProviderOperationOutcome.Unknown, null }
    };

    [Fact]
    public void Confirmation_replay_is_declared_safe_only_by_the_verified_capability()
    {
        var harness = new ReservationHarness();
        var order = harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);

        Assert.True(harness.FlightFlowReservation.CapabilityFor(ReservationHarness.AirService(order, 1, 101)).SupportsSafeConfirmReplay);
    }

    [Fact]
    public async Task Confirmation_posts_the_hold_id_without_an_idempotency_identity()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var provider = ProviderOver(handler);

        var request = provider.ConfirmRequestFor(new ConfirmationIntent(FulfillmentProviderKeys.FlightFlow, HoldId));
        await provider.ConfirmAsync(request);

        var (sent, body) = handler.Requests.Single();
        Assert.Equal((HttpMethod.Post, $"https://provider.test/service/v1/Flights/Seat-Holds/{HoldId}/Confirmations"), (sent.Method, sent.RequestUri!.ToString()));
        Assert.Empty(body);
        Assert.Null(request.IdempotencyKey);
        Assert.Equal(ProviderInteractionType.ConfirmHold, request.InteractionType);
    }

    [Theory]
    [MemberData(nameof(Responses))]
    public async Task Confirmation_response_maps_to_a_provider_neutral_outcome(
        HttpStatusCode status,
        string body,
        ProviderOperationOutcome expectedOutcome,
        ReservationMemberStatus? expectedStatus)
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        var outcome = await ConfirmAsync(handler);

        Assert.Equal((expectedOutcome, expectedStatus), (outcome.OperationOutcome, outcome.ObservedStatus));
        Assert.Equal<int?>((int)status, outcome.Response!.StatusCode);
    }

    [Fact]
    public async Task Timed_out_confirmation_is_an_unknown_outcome()
    {
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("The request timed out."));

        var outcome = await ConfirmAsync(handler);

        Assert.Equal((ProviderOperationOutcome.Unknown, FulfillmentFailureReason.UnknownOutcome), (outcome.OperationOutcome, outcome.Failure!.Reason));
    }

    [Fact]
    public async Task Connection_failure_during_confirmation_is_an_unknown_outcome()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("The connection was reset."));

        var outcome = await ConfirmAsync(handler);

        Assert.Equal(ProviderOperationOutcome.Unknown, outcome.OperationOutcome);
    }

    private static Task<ConfirmationOutcome> ConfirmAsync(StubHttpMessageHandler handler)
    {
        var provider = ProviderOver(handler);

        return provider.ConfirmAsync(provider.ConfirmRequestFor(new ConfirmationIntent(FulfillmentProviderKeys.FlightFlow, HoldId)));
    }

    private static FlightFlowReservationProvider ProviderOver(StubHttpMessageHandler handler)
        => new(new FlightFlowProvider(StubHttpMessageHandler.ClientFor(handler)), new StubAirFareReservationValidator(new FixedClock()));

    private static string ErrorBody(int code)
        => $$"""{"data":null,"errors":[{"code":{{code}},"title":"Cannot confirm the seat hold.","detail":null}]}""";
}
