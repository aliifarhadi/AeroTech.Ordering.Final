using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AncillaryProvision : ValueObject
    {
        private AncillaryProvision()
        {
        }

        public AncillaryProvision(
            long provisionId,
            PurchaseStage purchaseStage,
            BookingMethod bookingMethod,
            bool bookingRequired,
            ConfirmationRequirement confirmationRequirement,
            string? sourceFulfillmentProviderKey,
            DocumentRouting documentRouting,
            AncillaryDocumentType documentType,
            bool documentRequired,
            bool requiresCheckAtReserve)
        {
            ProvisionId = provisionId;
            PurchaseStage = purchaseStage;
            BookingMethod = bookingMethod;
            BookingRequired = bookingRequired;
            ConfirmationRequirement = confirmationRequirement;
            SourceFulfillmentProviderKey = sourceFulfillmentProviderKey;
            DocumentRouting = documentRouting;
            DocumentType = documentType;
            DocumentRequired = documentRequired;
            RequiresCheckAtReserve = requiresCheckAtReserve;
        }

        public long ProvisionId { get; private set; }

        public PurchaseStage PurchaseStage { get; private set; }

        public BookingMethod BookingMethod { get; private set; }

        public bool BookingRequired { get; private set; }

        public ConfirmationRequirement ConfirmationRequirement { get; private set; }

        public string? SourceFulfillmentProviderKey { get; private set; }

        public DocumentRouting DocumentRouting { get; private set; }

        public AncillaryDocumentType DocumentType { get; private set; }

        public bool DocumentRequired { get; private set; }

        public bool RequiresCheckAtReserve { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return ProvisionId;
            yield return PurchaseStage;
            yield return BookingMethod;
            yield return BookingRequired;
            yield return ConfirmationRequirement;
            yield return SourceFulfillmentProviderKey;
            yield return DocumentRouting;
            yield return DocumentType;
            yield return DocumentRequired;
            yield return RequiresCheckAtReserve;
        }
    }
}
