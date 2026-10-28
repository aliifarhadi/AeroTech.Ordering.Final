using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.Providers.Ancillary
{
    public sealed record ResolvedAncillarySelection(
        string SelectionId,
        ShoppingSourceKind SourceKind,
        string SourceReference,
        int? OrderCommercialVersion,
        DateTimeOffset ExpiresAt,
        int SelectedLineCount,
        IReadOnlyList<ResolvedAncillaryLine> Lines);

    public sealed record ResolvedAncillaryLine(
        string OfferItemId,
        AncillaryProfile Profile,
        string VariantCode,
        SelectionScope SelectionScope,
        ResolvedAncillaryUnit Unit,
        ResolvedAncillaryCoverage Coverage,
        ResolvedAncillaryDefinition Definition,
        ResolvedAncillaryProvision Provision,
        ResolvedAncillaryPricing Pricing,
        int SelectedQuantity,
        ResolvedAncillaryDetails Details);

    public sealed record ResolvedSeatQuote(
        ShoppingSourceKind SourceKind,
        string SourceReference,
        int? OrderCommercialVersion,
        DateTimeOffset ExpiresAt,
        IReadOnlyList<ResolvedSeat> Seats);

    public sealed record ResolvedSeat(
        string SeatSelectionId,
        string VariantCode,
        string TravellerRef,
        long FlightId,
        string SeatNumber,
        long PointOfSaleId,
        ResolvedAncillaryDefinition Definition,
        ResolvedAncillaryProvision Provision,
        ResolvedAncillaryPricing Pricing);

    public sealed record ResolvedAncillaryUnit(string? TravellerRef, string? PortionRef, string? FlightRef);

    public sealed record ResolvedAncillaryCoverage(
        IReadOnlyList<string> TravellerRefs,
        IReadOnlyList<string> PortionRefs,
        IReadOnlyList<string> FlightRefs);

    public sealed record ResolvedAncillaryDefinition(
        long DefinitionVersionId,
        string ServiceDefinitionRef,
        string VariantCode,
        string ServiceTypeCode,
        string ServiceSubCode,
        string? Rfic,
        string? Rfisc,
        string? SsrCode);

    public sealed record ResolvedAncillaryProvision(
        long ProvisionId,
        ServiceCoverageScope CoverageScope,
        PurchaseStage PurchaseStage,
        BookingMethod BookingMethod,
        bool BookingRequired,
        ConfirmationRequirement ConfirmationRequirement,
        string? FulfillmentProviderKey,
        DocumentRouting DocumentRouting,
        AncillaryDocumentType DocumentType,
        bool DocumentRequired,
        bool RequiresCheckAtReserve);

    public sealed record ResolvedAncillaryPricing(
        long PricingId,
        long PricingRateId,
        int PricingVersion,
        int CurrencyId,
        int Quantity,
        decimal UnitAmount,
        decimal TotalAmount,
        PricingUnit PricingUnit,
        decimal BaseAmount,
        bool IsUnitTotalComplete,
        IReadOnlyList<ResolvedAncillaryPriceLine> TaxLines,
        IReadOnlyList<ResolvedAncillaryPriceLine> FeeLines);

    public sealed record ResolvedAncillaryPriceLine(
        string? Code,
        decimal Amount,
        int CurrencyId,
        int? CountryId,
        int? StationAirportId,
        TaxTreatment? TaxTreatment,
        FeeApplicationUnit? FeeApplicationUnit);

    public sealed record ResolvedAncillaryDetails
    {
        public ResolvedBaggageDetails? Baggage { get; init; }

        public ResolvedMealDetails? Meal { get; init; }

        public ResolvedPetDetails? Pet { get; init; }

        public ResolvedAssistanceDetails? Assistance { get; init; }

        public ResolvedAirportServiceDetails? AirportService { get; init; }

        public ResolvedPriorityDetails? Priority { get; init; }

        public ResolvedConnectivityDetails? Connectivity { get; init; }

        public ResolvedUpgradeDetails? Upgrade { get; init; }

        public ResolvedExtraSeatDetails? ExtraSeat { get; init; }
    }

    public sealed record ResolvedDimensions(decimal LengthCm, decimal WidthCm, decimal HeightCm);

    public sealed record ResolvedBaggageDetails(
        BaggageChargeKind ChargeKind,
        BaggageAllowanceConcept? AllowanceConcept,
        decimal? PackageWeightKg,
        decimal? WeightFromExclusiveKg,
        decimal? WeightToInclusiveKg,
        string? EquipmentKind,
        string? BagRef,
        decimal? MeasuredWeightKg,
        ResolvedDimensions? Dimensions,
        decimal? WeightKg);

    public sealed record ResolvedMealDetails(
        MealKind MealKind,
        string? MealCode,
        string? MenuItemRef,
        string? DietaryCode,
        string? ExclusiveMealFamilyCode);

    public sealed record ResolvedPetDetails(
        PetTransportMode TransportMode,
        PetAnimalType? AnimalType,
        string? OtherAnimalCode,
        int? AnimalAgeWeeks,
        decimal? CombinedWeightKg,
        ResolvedDimensions? CarrierDimensions,
        IReadOnlyList<string> DocumentAcknowledgements,
        string? SizeBracket);

    public sealed record ResolvedAssistanceDetails(
        AssistanceKind AssistanceKind,
        string? AssistanceSsrCode,
        string? InfantRef,
        string? GuardianRef,
        ResolvedMedicalEquipmentDetails? MedicalEquipment,
        ResolvedUnaccompaniedMinorDetails? UnaccompaniedMinor);

    public sealed record ResolvedMedicalEquipmentDetails(
        string? EquipmentCode,
        IReadOnlyList<string> EvidenceDocumentRefs,
        decimal? OxygenUnits,
        decimal? OxygenFlowRateLitersPerMinute,
        string? FrequentTravellerMedicalCardNumber);

    public sealed record ResolvedUnaccompaniedMinorDetails(
        string? GuardianHandoffContact,
        string? GuardianPickupContact);

    public sealed record ResolvedAirportServiceDetails(
        AirportServiceKind Kind,
        int AirportId,
        string? TerminalRef,
        long? FacilityId,
        AirportServiceDirection Direction,
        DateTimeOffset? TimeWithOffset,
        int? GuestCount);

    public sealed record ResolvedPriorityDetails(
        PriorityKind Kind,
        string? PriorityZoneCode,
        string? PriorityGroupCode,
        string? FareBenefitRef,
        bool? OptIn);

    public sealed record ResolvedConnectivityDetails(
        ConnectivityPlanKind PlanKind,
        int? DurationMinutes,
        int? IncludedDataMb,
        int? MaxDevices,
        PurchaseStage DeliveryStage,
        int? DeviceCount);

    public sealed record ResolvedUpgradeDetails(
        int FromCabinId,
        int ToCabinId,
        UpgradeKind AllowedUpgradeKind,
        bool RequiresTicketExchange);

    public sealed record ResolvedExtraSeatDetails(
        SeatPurpose SeatPurpose,
        ExtraSeatPurpose? ExtraSeatPurpose,
        int? ExtraOccupiedSeatCount,
        string? SeatNumber);
}
