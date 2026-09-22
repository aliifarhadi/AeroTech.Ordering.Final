namespace AeroTech.Messages.Identity.IntegrationEvents.V1
{
    public sealed record IdentityLivenessHeartbeat(
        long SequenceNumber,
        DateTimeOffset EmittedAt) : BaseIntegrationEvent;
}
