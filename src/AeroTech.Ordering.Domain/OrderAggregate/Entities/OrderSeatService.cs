using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderSeatService : OrderService
    {
        private OrderSeatService()
        {
        }

        public OrderSeatService(
            long id,
            long orderId,
            long orderItemId,
            long travellerId,
            long segmentId,
            long associatedAirServiceId,
            string seatNumber,
            AncillaryVariant variant,
            long pointOfSaleId,
            AncillarySaleSource source,
            AncillaryDefinition definition,
            AncillaryProvision provision,
            AncillaryPricing pricing,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : base(id, orderId, orderItemId, travellerId, OrderServiceType.SeatAssignment, FulfillmentProviderKeys.FlightFlow, createdByChangeId, createdAt)
        {
            if (!variant.IsSeatAssignment)
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(source.SelectionId, variant.Code, variant.Profile);

            SegmentId = segmentId;
            AssociatedAirServiceId = associatedAirServiceId;
            SeatNumber = seatNumber;
            VariantCode = variant.Code;
            PointOfSaleId = pointOfSaleId;
            Source = source;
            Definition = definition;
            Provision = provision;
            Pricing = pricing;
        }

        public long SegmentId { get; private set; }

        public long AssociatedAirServiceId { get; private set; }

        public string SeatNumber { get; private set; } = default!;

        public string? VariantCode { get; private set; }

        public long? PointOfSaleId { get; private set; }

        public AncillarySaleSource? Source { get; private set; }

        public AncillaryDefinition? Definition { get; private set; }

        public AncillaryProvision? Provision { get; private set; }

        public AncillaryPricing? Pricing { get; private set; }

        public bool IsPricedSelection => Pricing is not null;
    }
}
