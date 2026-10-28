using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderAncillaryServiceReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public string VariantCode { get; set; } = string.Empty;

        public AncillaryProfile Profile { get; set; }

        public int Quantity { get; set; }

        public SelectionScope? SelectionScope { get; set; }

        public ServiceCoverageScope? CoverageScope { get; set; }

        public List<long> CoveredAirServiceIds { get; set; } = [];

        public long? PointOfSaleId { get; set; }

        public ShoppingSourceKind SourceKind { get; set; }

        public string SourceReference { get; set; } = string.Empty;

        public int? SourceOrderCommercialVersion { get; set; }

        public string SourceSelectionId { get; set; } = string.Empty;

        public string? SourceOfferItemId { get; set; }

        public long DefinitionVersionId { get; set; }

        public string ServiceDefinitionRef { get; set; } = string.Empty;

        public string ServiceTypeCode { get; set; } = string.Empty;

        public string ServiceSubCode { get; set; } = string.Empty;

        public string? Rfic { get; set; }

        public string? Rfisc { get; set; }

        public string? SsrCode { get; set; }

        public long ProvisionId { get; set; }

        public PurchaseStage PurchaseStage { get; set; }

        public BookingMethod BookingMethod { get; set; }

        public bool BookingRequired { get; set; }

        public ConfirmationRequirement ConfirmationRequirement { get; set; }

        public string? ProvisionFulfillmentProviderKey { get; set; }

        public DocumentRouting DocumentRouting { get; set; }

        public AncillaryDocumentType DocumentType { get; set; }

        public bool DocumentRequired { get; set; }

        public bool RequiresCheckAtReserve { get; set; }

        public long PricingId { get; set; }

        public long PricingRateId { get; set; }

        public int PricingVersion { get; set; }

        public PricingUnit PricingUnit { get; set; }

        public BaggageChargeKind? BaggageChargeKind { get; set; }

        public BaggageAllowanceConcept? BaggageAllowanceConcept { get; set; }

        public decimal? BaggagePackageWeightKg { get; set; }

        public decimal? BaggageWeightFromExclusiveKg { get; set; }

        public decimal? BaggageWeightToInclusiveKg { get; set; }

        public string? BaggageEquipmentKind { get; set; }

        public string? BaggageBagRef { get; set; }

        public decimal? BaggageMeasuredWeightKg { get; set; }

        public decimal? BaggageLengthCm { get; set; }

        public decimal? BaggageWidthCm { get; set; }

        public decimal? BaggageHeightCm { get; set; }

        public decimal? BaggageWeightKg { get; set; }

        public MealKind? MealKind { get; set; }

        public string? MealCode { get; set; }

        public string? MealMenuItemRef { get; set; }

        public string? MealDietaryCode { get; set; }

        public string? MealExclusiveFamilyCode { get; set; }

        public PetTransportMode? PetTransportMode { get; set; }

        public PetAnimalType? PetAnimalType { get; set; }

        public string? PetOtherAnimalCode { get; set; }

        public int? PetAnimalAgeWeeks { get; set; }

        public decimal? PetCombinedWeightKg { get; set; }

        public decimal? PetCarrierLengthCm { get; set; }

        public decimal? PetCarrierWidthCm { get; set; }

        public decimal? PetCarrierHeightCm { get; set; }

        public List<string> PetDocumentAcknowledgements { get; set; } = [];

        public string? PetSizeBracket { get; set; }

        public AssistanceKind? AssistanceKind { get; set; }

        public string? AssistanceSsrCode { get; set; }

        public long? AssistanceInfantTravellerId { get; set; }

        public long? AssistanceGuardianTravellerId { get; set; }

        public AirportServiceKind? AirportServiceKind { get; set; }

        public int? AirportServiceAirportId { get; set; }

        public string? AirportServiceTerminalRef { get; set; }

        public long? AirportServiceFacilityId { get; set; }

        public AirportServiceDirection? AirportServiceDirection { get; set; }

        public DateTimeOffset? AirportServiceTimeWithOffset { get; set; }

        public int? AirportServiceGuestCount { get; set; }

        public PriorityKind? PriorityKind { get; set; }

        public string? PriorityZoneCode { get; set; }

        public string? PriorityGroupCode { get; set; }

        public string? PriorityFareBenefitRef { get; set; }

        public bool? PriorityOptIn { get; set; }

        public ConnectivityPlanKind? ConnectivityPlanKind { get; set; }

        public int? ConnectivityDurationMinutes { get; set; }

        public int? ConnectivityIncludedDataMb { get; set; }

        public int? ConnectivityMaxDevices { get; set; }

        public PurchaseStage? ConnectivityDeliveryStage { get; set; }

        public int? ConnectivityDeviceCount { get; set; }

        public int? UpgradeFromCabinId { get; set; }

        public int? UpgradeToCabinId { get; set; }

        public UpgradeKind? UpgradeKind { get; set; }

        public bool? UpgradeRequiresTicketExchange { get; set; }
    }
}
