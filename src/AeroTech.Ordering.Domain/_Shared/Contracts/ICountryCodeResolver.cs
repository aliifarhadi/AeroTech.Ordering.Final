namespace AeroTech.Ordering.Domain._Shared.Contracts
{
    public interface ICountryCodeResolver
    {
        Task<int> ResolveAsync(string alpha2Code, CancellationToken cancellationToken = default);

        Task<int?> ResolveOptionalAsync(string? alpha2Code, CancellationToken cancellationToken = default);
    }
}
