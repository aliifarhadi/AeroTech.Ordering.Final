using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Providers.Ancillary.Wire
{
    public sealed class AncillaryEnvelope<T>
    {
        public T? Data { get; set; }

        public AncillaryResultError[]? Errors { get; set; }
    }

    public sealed class AncillaryResultError
    {
        public int Code { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }
    }

    public sealed class ResolvedSelectionsResponse
    {
        public long? OrderId { get; set; }

        public int? OrderCommercialVersion { get; set; }

        public ShoppingSourceKind? SourceKind { get; set; }

        public string? SourceReference { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public int? SelectedLineCount { get; set; }

        public List<ResolvedSelection<BaggageDetails>> Baggages { get; set; } = new();

        public List<ResolvedSelection<MealDetails>> Meals { get; set; } = new();

        public List<ResolvedSelection<PetDetails>> Pets { get; set; } = new();

        public List<ResolvedSelection<AssistedTravelDetails>> AssistedTravel { get; set; } = new();

        public List<ResolvedSelection<AirportServiceDetails>> AirportServices { get; set; } = new();

        public List<ResolvedSelection<PriorityDetails>> PriorityServices { get; set; } = new();

        public List<ResolvedSelection<ConnectivityDetails>> Connectivity { get; set; } = new();

        public List<ResolvedSelection<UpgradeDetails>> Upgrades { get; set; } = new();

        public List<ResolvedSelection<ExtraSeatDetails>> ExtraSeats { get; set; } = new();

        public List<ResolvedSelection<TravelInsuranceDetails>> TravelInsurance { get; set; } = new();
    }

    public sealed class ResolvedSelection<TDetails>
    {
        public string OfferItemId { get; set; } = string.Empty;

        public string VariantCode { get; set; } = string.Empty;

        public SelectionScope SelectionScope { get; set; }

        public SelectionUnit Unit { get; set; } = new();

        public SelectionCoverage Coverage { get; set; } = new();

        public ResolvedDefinition Definition { get; set; } = new();

        public ResolvedProvision Provision { get; set; } = new();

        public ResolvedPricing Pricing { get; set; } = new();

        public TDetails? Details { get; set; }

        public SelectedUnit Selected { get; set; } = new();
    }

    public sealed class SelectionUnit
    {
        public string? Scope { get; set; }

        public string? TravellerRef { get; set; }

        public string? PortionRef { get; set; }

        public string? FlightRef { get; set; }
    }

    public sealed class SelectionCoverage
    {
        public List<string> TravellerRefs { get; set; } = new();

        public List<string> PortionRefs { get; set; } = new();

        public List<string> FlightRefs { get; set; } = new();
    }

    public sealed class ResolvedDefinition
    {
        public long DefinitionVersionId { get; set; }

        public string ServiceDefinitionRef { get; set; } = string.Empty;

        public string VariantCode { get; set; } = string.Empty;

        public string ServiceTypeCode { get; set; } = string.Empty;

        public string ServiceSubCode { get; set; } = string.Empty;

        public string? Rfic { get; set; }

        public string? Rfisc { get; set; }

        public string? SsrCode { get; set; }
    }

    public sealed class ResolvedProvision
    {
        public long ProvisionId { get; set; }

        public ServiceCoverageScope CoverageScope { get; set; }

        public PurchaseStage PurchaseStage { get; set; }

        public BookingMethod BookingMethod { get; set; }

        public bool BookingRequired { get; set; }

        public ConfirmationRequirement ConfirmationRequirement { get; set; }

        public string? FulfillmentProviderKey { get; set; }

        public DocumentRouting DocumentRouting { get; set; }

        public AncillaryDocumentType DocumentType { get; set; }

        public bool DocumentRequired { get; set; }

        public bool RequiresCheckAtReserve { get; set; }
    }

    public sealed class ResolvedPricing
    {
        public long PricingId { get; set; }

        public long PricingRateId { get; set; }

        public int PricingVersion { get; set; }

        public int CurrencyId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public PricingUnit PricingUnit { get; set; }

        public decimal BaseAmount { get; set; }

        public bool? IsUnitTotalComplete { get; set; }

        public List<PriceLine> TaxLines { get; set; } = new();

        public List<PriceLine> FeeLines { get; set; } = new();
    }

    public sealed class PriceLine
    {
        public string? Code { get; set; }

        public decimal Amount { get; set; }

        public int CurrencyId { get; set; }

        public int? CountryId { get; set; }

        public int? StationAirportId { get; set; }

        public TaxTreatment? TaxTreatment { get; set; }

        public FeeApplicationUnit? FeeApplicationUnit { get; set; }
    }

    public sealed class SelectedUnit
    {
        public int Quantity { get; set; }

        public SelectedOptions? Options { get; set; }
    }

    public sealed class SelectedOptions
    {
        public BaggageOptions? Baggage { get; set; }

        public PetOptions? Pet { get; set; }

        public AssistedTravelOptions? AssistedTravel { get; set; }

        public AirportServiceOptions? AirportService { get; set; }

        public PriorityOptions? Priority { get; set; }

        public ConnectivityOptions? Connectivity { get; set; }
    }

    public sealed class Dimensions
    {
        public decimal LengthCm { get; set; }

        public decimal WidthCm { get; set; }

        public decimal HeightCm { get; set; }
    }

    public sealed class BaggageOptions
    {
        public string? BagRef { get; set; }

        public decimal? MeasuredWeightKg { get; set; }

        public Dimensions? Dimensions { get; set; }

        public decimal? WeightKg { get; set; }
    }

    public sealed class PetOptions
    {
        public PetAnimalType? AnimalType { get; set; }

        public string? OtherAnimalCode { get; set; }

        public int? AnimalAgeWeeks { get; set; }

        public decimal? CombinedWeightKg { get; set; }

        public Dimensions? CarrierDimensions { get; set; }

        public List<string>? DocumentAcknowledgements { get; set; }

        public string? SizeBracket { get; set; }
    }

    public sealed class AssistedTravelOptions
    {
        public string? AssistanceSsrCode { get; set; }

        public string? InfantRef { get; set; }

        public string? GuardianRef { get; set; }
    }

    public sealed class AirportServiceOptions
    {
        public DateTimeOffset? TimeWithOffset { get; set; }

        public int? GuestCount { get; set; }
    }

    public sealed class PriorityOptions
    {
        public bool? OptIn { get; set; }
    }

    public sealed class ConnectivityOptions
    {
        public int? DeviceCount { get; set; }
    }

    public sealed class BaggageDetails
    {
        public BaggageChargeKind ChargeKind { get; set; }

        public BaggageAllowanceConcept? AllowanceConcept { get; set; }

        public decimal? PackageWeightKg { get; set; }

        public decimal? WeightFromExclusiveKg { get; set; }

        public decimal? WeightToInclusiveKg { get; set; }

        public string? EquipmentKind { get; set; }
    }

    public sealed class MealDetails
    {
        public MealKind MealKind { get; set; }

        public string? MealCode { get; set; }

        public string? MenuItemRef { get; set; }

        public string? DietaryCode { get; set; }

        public string? ExclusiveMealFamilyCode { get; set; }
    }

    public sealed class PetDetails
    {
        public PetTransportMode TransportMode { get; set; }
    }

    public sealed class AssistedTravelDetails
    {
        public AssistanceKind AssistanceKind { get; set; }
    }

    public sealed class AirportServiceDetails
    {
        public AirportServiceKind Kind { get; set; }

        public int AirportId { get; set; }

        public string? TerminalRef { get; set; }

        public long? FacilityId { get; set; }

        public AirportServiceDirection Direction { get; set; }
    }

    public sealed class PriorityDetails
    {
        public PriorityKind Kind { get; set; }

        public string? PriorityZoneCode { get; set; }

        public string? PriorityGroupCode { get; set; }

        public string? FareBenefitRef { get; set; }
    }

    public sealed class ConnectivityDetails
    {
        public ConnectivityPlanKind PlanKind { get; set; }

        public int? DurationMinutes { get; set; }

        public int? IncludedDataMb { get; set; }

        public int? MaxDevices { get; set; }

        public PurchaseStage DeliveryStage { get; set; }
    }

    public sealed class UpgradeDetails
    {
        public int FromCabinId { get; set; }

        public int ToCabinId { get; set; }

        public UpgradeKind AllowedUpgradeKind { get; set; }

        public bool RequiresTicketExchange { get; set; }
    }

    public sealed class ExtraSeatDetails
    {
        public SeatPurpose SeatPurpose { get; set; }
    }

    public sealed class TravelInsuranceDetails
    {
    }
}
