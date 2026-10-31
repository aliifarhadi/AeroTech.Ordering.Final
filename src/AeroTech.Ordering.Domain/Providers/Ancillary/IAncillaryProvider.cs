namespace AeroTech.Ordering.Domain.Providers.Ancillary
{
    public interface IAncillaryProvider
    {
        Task<ResolvedAncillarySelection> ResolveSelectionAsync(string selectionId, CancellationToken cancellationToken = default);

        Task<ResolvedSeatQuote> ResolveSeatSelectionAsync(string selectionId, CancellationToken cancellationToken = default);
    }
}
