namespace AeroTech.Ordering.Domain.Providers.FlightFlow
{
    public sealed record FlightFlowReply<T>(T Result, int StatusCode, string Body);
}
