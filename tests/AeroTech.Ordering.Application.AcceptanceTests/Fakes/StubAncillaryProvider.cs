using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubAncillaryProvider : IAncillaryProvider
{
    public Dictionary<string, ResolvedAncillarySelection> Selections { get; } = [];

    public List<string> Requests { get; } = [];

    public Task<ResolvedAncillarySelection> ResolveSelectionAsync(string selectionId, CancellationToken cancellationToken = default)
    {
        Requests.Add(selectionId);

        return Selections.TryGetValue(selectionId, out var selection)
            ? Task.FromResult(selection)
            : throw ExceptionFactory.AncillarySelectionIsNotFound(selectionId);
    }
}
