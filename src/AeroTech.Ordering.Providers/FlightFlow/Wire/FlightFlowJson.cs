using System.Text.Json;
using System.Text.Json.Serialization;

namespace AeroTech.Ordering.Providers.FlightFlow.Wire
{
    public static class FlightFlowJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }
}
