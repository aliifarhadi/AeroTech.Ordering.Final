namespace AeroTech.Messages.Core.IntegrationEvents.V1
{
    public sealed record CoreLivenessHeartbeat(
        long SequenceNumber,
        DateTimeOffset EmittedAt) : BaseIntegrationEvent;
}
