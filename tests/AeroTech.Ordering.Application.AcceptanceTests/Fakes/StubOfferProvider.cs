using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class StubOfferProvider : IOfferProvider
{
    public Dictionary<string, OfferDetail> Offers { get; } = [];

    public List<string> Requests { get; } = [];

    public Task<OfferDetail> GetByOfferIdAsync(string offerId, CancellationToken cancellationToken = default)
    {
        Requests.Add(offerId);

        return Offers.TryGetValue(offerId, out var offer)
            ? Task.FromResult(offer)
            : throw ExceptionFactory.OfferCouldNotBeRetrieved(null, offerId);
    }
}
