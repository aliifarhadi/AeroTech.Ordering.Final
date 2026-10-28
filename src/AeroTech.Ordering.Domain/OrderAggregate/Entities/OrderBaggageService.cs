using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderBaggageService : OrderAncillaryService
    {
        private OrderBaggageService()
        {
        }

        public OrderBaggageService(
            long id,
            long orderId,
            long orderItemId,
            long? travellerId,
            AncillaryVariant variant,
            int quantity,
            SelectionScope selectionScope,
            ServiceCoverageScope coverageScope,
            IReadOnlyCollection<long> coveredAirServiceIds,
            AncillarySaleSource source,
            AncillaryDefinition definition,
            AncillaryProvision provision,
            AncillaryPricing pricing,
            BaggageChoice baggage,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : base(
                id,
                orderId,
                orderItemId,
                travellerId,
                variant,
                quantity,
                selectionScope,
                coverageScope,
                coveredAirServiceIds,
                source,
                definition,
                provision,
                pricing,
                createdByChangeId,
                createdAt)
        {
            if (variant.Profile != AncillaryProfile.Baggage)
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(source.OfferItemId, variant.Code, variant.Profile);

            Baggage = baggage;
        }

        public BaggageChoice Baggage { get; private set; } = default!;
    }
}
