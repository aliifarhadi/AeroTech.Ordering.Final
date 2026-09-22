namespace AeroTech.Framework.Core.ServiceContracts
{
    public sealed record Actor(string? Subject, long? ActorId, string? ActorType)
    {
        public static readonly Actor Anonymous = new(null, null, null);

        public bool IsAuthenticated => Subject is not null;
    }

    public interface IActorResolver
    {
        Actor Resolve();
    }
}
