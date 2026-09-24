using System.Net;
using System.Text;
using System.Text.Json;
using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Providers.Pricing.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Pricing;

public sealed class PricingProviderTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Request_is_posted_to_bound_reservation_validation_with_numeric_enums()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, """{"data":{"timeLimit":"2026-10-02T10:00:00+00:00"},"errors":null}"""));

        var timeLimit = await ValidateAsync(handler);

        var (request, body) = handler.Requests.Single();
        using var json = JsonDocument.Parse(body);
        var unit = json.RootElement.GetProperty("pricingUnits")[0];

        Assert.Equal(new DateTimeOffset(2026, 10, 2, 10, 0, 0, TimeSpan.Zero), timeLimit);
        Assert.Equal("https://provider.test/service/v1/BoundReservationValidation", request.RequestUri!.ToString());
        Assert.Equal(1, json.RootElement.GetProperty("salesContext").GetProperty("channel").GetInt32());
        Assert.Equal(1, unit.GetProperty("journeyType").GetInt32());
        Assert.Equal(2, unit.GetProperty("airFares")[0].GetProperty("flights")[0].GetProperty("flightStatus").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("passengers")[0].GetProperty("passengerTypeCode").GetInt32());
    }

    [Fact]
    public async Task Fare_rule_rejection_is_not_permitted()
    {
        var handler = new StubHttpMessageHandler(_ => Json(
            HttpStatusCode.BadRequest,
            """{"data":null,"errors":[{"code":1411,"title":"The flight is not valid for the journey type.","detail":null}]}"""));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(handler));

        Assert.Equal(2608, exception.Code);
        Assert.Equal(422, exception.HttpStatus);
        Assert.Equal("The flight is not valid for the journey type.", exception.Message);
    }

    public static TheoryData<HttpStatusCode, string> UpstreamFailures => new()
    {
        { HttpStatusCode.BadRequest, """{"data":null,"errors":[{"title":"Service communication error"}]}""" },
        { HttpStatusCode.InternalServerError, string.Empty },
        { HttpStatusCode.BadRequest, """{"type":"about:blank","errors":{"PricingUnits":["The PricingUnits field is required."]}}""" }
    };

    [Theory]
    [MemberData(nameof(UpstreamFailures))]
    public async Task Upstream_failure_is_reported_as_unvalidated(HttpStatusCode status, string body)
    {
        var handler = new StubHttpMessageHandler(_ => Json(status, body));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(handler));

        Assert.Equal(2607, exception.Code);
        Assert.Equal(502, exception.HttpStatus);
    }

    [Fact]
    public async Task Unreachable_AirPrice_is_reported_as_unvalidated()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Connection refused."));

        var exception = await Assert.ThrowsAsync<BusinessException>(() => ValidateAsync(handler));

        Assert.Equal(2607, exception.Code);
        Assert.Equal(502, exception.HttpStatus);
    }

    private Task<DateTimeOffset> ValidateAsync(StubHttpMessageHandler handler)
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        var validator = new AirFareReservationValidator(new PricingProvider(StubHttpMessageHandler.ClientFor(handler)), _harness.Clock);

        return validator.ValidateAsync(order, ReservationHarness.AirServices(order).Select(service => service.Id).ToList());
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
