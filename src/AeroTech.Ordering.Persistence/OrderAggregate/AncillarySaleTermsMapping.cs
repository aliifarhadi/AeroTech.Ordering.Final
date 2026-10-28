using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    internal static class AncillarySaleTermsMapping
    {
        public static void MapSource<TOwner>(OwnedNavigationBuilder<TOwner, AncillarySaleSource> source)
            where TOwner : class
        {
            source.Property(value => value.Kind).HasColumnName("SourceKind");
            source.Property(value => value.Reference).HasColumnName("SourceReference").HasMaxLength(512);
            source.Property(value => value.OrderCommercialVersion).HasColumnName("SourceOrderCommercialVersion");
            source.Property(value => value.SelectionId).HasColumnName("SourceSelectionId").HasMaxLength(1024);
            source.Property(value => value.OfferItemId).HasColumnName("SourceOfferItemId").HasMaxLength(1024);
        }

        public static void MapDefinition<TOwner>(OwnedNavigationBuilder<TOwner, AncillaryDefinition> definition)
            where TOwner : class
        {
            definition.Property(value => value.DefinitionVersionId).HasColumnName("DefinitionVersionId");
            definition.Property(value => value.ServiceDefinitionRef).HasColumnName("ServiceDefinitionRef").HasMaxLength(128);
            definition.Property(value => value.ServiceTypeCode).HasColumnName("ServiceTypeCode").HasMaxLength(8);
            definition.Property(value => value.ServiceSubCode).HasColumnName("ServiceSubCode").HasMaxLength(8);
            definition.Property(value => value.Rfic).HasColumnName("Rfic").HasMaxLength(8);
            definition.Property(value => value.Rfisc).HasColumnName("Rfisc").HasMaxLength(8);
            definition.Property(value => value.SsrCode).HasColumnName("SsrCode").HasMaxLength(8);
        }

        public static void MapProvision<TOwner>(OwnedNavigationBuilder<TOwner, AncillaryProvision> provision)
            where TOwner : class
        {
            provision.Property(value => value.ProvisionId).HasColumnName("ProvisionId");
            provision.Property(value => value.PurchaseStage).HasColumnName("ProvisionPurchaseStage");
            provision.Property(value => value.BookingMethod).HasColumnName("ProvisionBookingMethod");
            provision.Property(value => value.BookingRequired).HasColumnName("ProvisionBookingRequired");
            provision.Property(value => value.ConfirmationRequirement).HasColumnName("ProvisionConfirmationRequirement");
            provision.Property(value => value.SourceFulfillmentProviderKey).HasColumnName("ProvisionFulfillmentProviderKey").HasMaxLength(64);
            provision.Property(value => value.DocumentRouting).HasColumnName("ProvisionDocumentRouting");
            provision.Property(value => value.DocumentType).HasColumnName("ProvisionDocumentType");
            provision.Property(value => value.DocumentRequired).HasColumnName("ProvisionDocumentRequired");
            provision.Property(value => value.RequiresCheckAtReserve).HasColumnName("ProvisionRequiresCheckAtReserve");
        }

        public static void MapPricing<TOwner>(OwnedNavigationBuilder<TOwner, AncillaryPricing> pricing)
            where TOwner : class
        {
            pricing.Property(value => value.PricingId).HasColumnName("PricingId");
            pricing.Property(value => value.PricingRateId).HasColumnName("PricingRateId");
            pricing.Property(value => value.PricingVersion).HasColumnName("PricingVersion");
            pricing.Property(value => value.PricingUnit).HasColumnName("PricingUnit");
        }
    }
}
