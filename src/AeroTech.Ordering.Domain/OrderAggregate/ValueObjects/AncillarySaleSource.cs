using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AncillarySaleSource : ValueObject
    {
        private AncillarySaleSource()
        {
        }

        public AncillarySaleSource(
            ShoppingSourceKind kind,
            string reference,
            int? orderCommercialVersion,
            string selectionId,
            string? offerItemId)
        {
            Kind = kind;
            Reference = reference;
            OrderCommercialVersion = orderCommercialVersion;
            SelectionId = selectionId;
            OfferItemId = offerItemId;
        }

        public ShoppingSourceKind Kind { get; private set; }

        public string Reference { get; private set; } = default!;

        public int? OrderCommercialVersion { get; private set; }

        public string SelectionId { get; private set; } = default!;

        public string? OfferItemId { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Kind;
            yield return Reference;
            yield return OrderCommercialVersion;
            yield return SelectionId;
            yield return OfferItemId;
        }
    }
}
