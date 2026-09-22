
namespace AeroTech.Ordering.Domain.Providers.Offer
{
    public interface IOfferProvider
    {
        Task<OfferDetail> GetByOfferIdAsync(string offerId, CancellationToken cancellationToken = default);
    }
}
