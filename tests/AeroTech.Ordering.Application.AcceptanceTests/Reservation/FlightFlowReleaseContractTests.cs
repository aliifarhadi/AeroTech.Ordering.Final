using System.Net;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain.Providers.Reservation;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class FlightFlowReleaseContractTests
{
    private const string HoldId = "HOLD-1";

    public static TheoryData<HttpStatusCode, string, ProviderOperationOutcome, FulfillmentReservationStatus?> Responses => new()
    {
        { HttpStatusCode.NoContent, string.Empty, ProviderOperationOutcome.Succeeded, FulfillmentReservationStatus.Released },
        { HttpStatusCode.OK, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.Created, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.Accepted, string.Empty, ProviderOperationOutcome.Unknown, null },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1181), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Unknown },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1182), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Confirmed },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1183), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Expired },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1184), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Cancelled },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1185), ProviderOperationOutcome.Rejected, FulfillmentReservationStatus.Mixed },
        { HttpStatusCode.BadRequest, FlightFlowWire.ErrorBody(1166), ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.Forbidden, string.Empty, ProviderOperationOutcome.Rejected, null },
        { HttpStatusCode.InternalServerError, string.Empty, ProviderOperationOutcome.Unknown, null }
    };

    [Fact]
    public async Task Release_deletes_the_hold_by_its_id()
    {
        var handler = new StubHttpMessageHandler(_ => FlightFlowWire.Response(HttpStatusCode.NoContent));

        await ReleaseAsync(handler);

        var (sent, body) = handler.Requests.Single();
        Assert.Equal((HttpMethod.Delete, $"https://provider.test/service/v1/Flights/Seat-Holds/{HoldId}"), (sent.Method, sent.RequestUri!.ToString()));
        Assert.Empty(body);
    }

    [Theory]
    [MemberData(nameof(Responses))]
    public async Task Release_response_maps_to_a_provider_neutral_outcome(
        HttpStatusCode status,
        string body,
        ProviderOperationOutcome expectedOutcome,
        FulfillmentReservationStatus? expectedStatus)
    {
        var handler = new StubHttpMessageHandler(_ => FlightFlowWire.Response(status, body));

        var outcome = await ReleaseAsync(handler);

        Assert.Equal((expectedOutcome, expectedStatus), (outcome.OperationOutcome, outcome.ObservedStatus));
        Assert.Equal<int?>((int)status, outcome.Response!.StatusCode);
    }

    private static Task<ReleaseOutcome> ReleaseAsync(StubHttpMessageHandler handler)
    {
        var provider = new FlightFlowReservationProvider(
            new FlightFlowProvider(StubHttpMessageHandler.ClientFor(handler)),
            new StubAirFareReservationValidator(new FixedClock()));

        return provider.ReleaseAsync(provider.ReleaseRequestFor(new ReleaseIntent(FulfillmentProviderKeys.FlightFlow, HoldId, "release-hold:1")));
    }
}
