using System.Net;
using System.Text;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public static class FlightFlowWire
{
    public static HttpResponseMessage Response(HttpStatusCode status, string body = "")
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static string ErrorBody(int code)
        => $$"""{"data":null,"errors":[{"code":{{code}},"title":"Cannot confirm the seat hold.","detail":null}]}""";
}
