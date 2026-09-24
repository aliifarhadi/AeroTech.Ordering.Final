using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Ordering.Providers.Offer.Wire;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public static class CapturedOffers
{
    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string RawBeforeBoundIdentity() => Read("offer-detail-round-trip-one-way-units.json");

    public static string RawWithBoundIdentity() => Read("offer-detail-round-trip-one-way-units.with-bound-identity.json");

    public static FlightOfferDetailResponse WithBoundIdentity() => Parse(RawWithBoundIdentity());

    public static FlightOfferDetailResponse Parse(string raw)
        => JsonSerializer.Deserialize<OfferEnvelope<FlightOfferDetailResponse>>(raw, WireOptions)!.Data!;

    private static string Read(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Offer", "Payloads", fileName));
}
