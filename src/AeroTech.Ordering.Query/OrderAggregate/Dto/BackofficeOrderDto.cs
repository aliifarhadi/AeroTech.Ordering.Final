using AeroTech.Ordering.Query._Shared.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Dto
{
    public sealed record BackofficeOrderDto(
        long Id,
        Guid OrderReference,
        string? RecordLocator,
        string SourceOfferId,
        EnumValueDto Status,
        EnumValueDto Channel,
        long CustomerId,
        SellingOfficeDto? SellingOffice,
        int CurrencyId,
        decimal CustomerTotal,
        int CommercialVersion,
        DateTimeOffset? LastTicketingDate,
        DateTimeOffset CreatedAt,
        IReadOnlyList<BackofficeOrderTravellerDto> Travellers,
        IReadOnlyList<OrderJourneyDto> Journeys,
        IReadOnlyList<BackofficeOrderItemDto> Items,
        IReadOnlyList<BackofficeOrderPricingLineDto> PricingLines,
        IReadOnlyList<BackofficeOrderContactDto> Contacts,
        IReadOnlyList<BackofficeOrderRemarkDto> Remarks,
        IReadOnlyList<BackofficeElectronicTicketDto> Tickets,
        IReadOnlyList<BackofficeOrderChangeDto> Changes);

    public sealed record SellingOfficeDto(EnumValueDto Kind, long Id, string? Code, string? Name);

    public sealed record BackofficeOrderTravellerDto(
        long Id,
        int Index,
        EnumValueDto PassengerType,
        EnumValueDto AgeRange,
        EnumValueDto Status,
        string GivenName,
        string? Surname,
        DateOnly? DateOfBirth,
        EnumValueDto? Gender,
        int? NationalityId,
        int? CountryOfResidenceId,
        long? InfantParentTravellerId,
        IReadOnlyList<BackofficeOrderTravellerDocumentDto> Documents);

    public sealed record BackofficeOrderTravellerDocumentDto(
        long Id,
        EnumValueDto Type,
        string Number,
        DateOnly? ExpiryDate,
        int IssuanceCountryId,
        string Holder);

    public sealed record BackofficeOrderItemDto(
        long Id,
        EnumValueDto Kind,
        decimal AcceptedTotal,
        EnumValueDto CommercialStatus,
        long? EndedByChangeId,
        IReadOnlyList<BackofficeOrderServiceDto> Services);

    public sealed record BackofficeOrderServiceDto(
        long Id,
        long? TravellerId,
        long? SegmentId,
        EnumValueDto ServiceType,
        EnumValueDto CommercialStatus,
        string? BookingClass,
        string? FareBasis,
        string? FareFamily,
        EnumValueDto? FareType,
        BackofficeBaggageAllowanceDto? CheckedBaggage,
        BackofficeBaggageAllowanceDto? CabinBaggage,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        string? SeatNumber,
        long? EndedByChangeId,
        BackofficeOrderAncillaryDto? Ancillary);

    public sealed record BackofficeBaggageAllowanceDto(int Pieces, decimal Weight, EnumValueDto Unit);

    public sealed record BackofficeOrderAncillaryDto(
        string VariantCode,
        EnumValueDto Profile,
        int Quantity,
        EnumValueDto? SelectionScope,
        EnumValueDto? CoverageScope,
        IReadOnlyList<long> CoveredAirServiceIds,
        string ServiceDefinitionRef,
        string ServiceTypeCode,
        string ServiceSubCode,
        string? Rfic,
        string? Rfisc,
        string? SsrCode,
        BackofficeAncillaryBaggageDto? Baggage,
        BackofficeAncillaryMealDto? Meal,
        BackofficeAncillaryPetDto? Pet,
        BackofficeAncillaryAssistanceDto? Assistance,
        BackofficeAncillaryAirportServiceDto? AirportService,
        BackofficeAncillaryPriorityDto? Priority,
        BackofficeAncillaryConnectivityDto? Connectivity,
        BackofficeAncillaryUpgradeDto? Upgrade);

    public sealed record BackofficeAncillaryBaggageDto(
        EnumValueDto ChargeKind,
        EnumValueDto? AllowanceConcept,
        decimal? PackageWeightKg,
        decimal? WeightFromExclusiveKg,
        decimal? WeightToInclusiveKg,
        string? EquipmentKind,
        string? BagRef,
        decimal? MeasuredWeightKg,
        OrderAncillaryDimensionsDto? Dimensions,
        decimal? WeightKg);

    public sealed record BackofficeAncillaryMealDto(
        EnumValueDto MealKind,
        string? MealCode,
        string? MenuItemRef,
        string? DietaryCode,
        string? ExclusiveMealFamilyCode);

    public sealed record BackofficeAncillaryPetDto(
        EnumValueDto TransportMode,
        EnumValueDto AnimalType,
        string? OtherAnimalCode,
        int? AnimalAgeWeeks,
        decimal CombinedWeightKg,
        OrderAncillaryDimensionsDto CarrierDimensions,
        IReadOnlyList<string> DocumentAcknowledgements,
        string? SizeBracket);

    public sealed record BackofficeAncillaryAssistanceDto(
        EnumValueDto AssistanceKind,
        string? AssistanceSsrCode,
        long? InfantTravellerId,
        long? GuardianTravellerId);

    public sealed record BackofficeAncillaryAirportServiceDto(
        EnumValueDto Kind,
        int AirportId,
        string? TerminalRef,
        long? FacilityId,
        EnumValueDto Direction,
        DateTimeOffset? TimeWithOffset,
        int? GuestCount);

    public sealed record BackofficeAncillaryPriorityDto(
        EnumValueDto Kind,
        string? PriorityZoneCode,
        string? PriorityGroupCode,
        string? FareBenefitRef,
        bool OptIn);

    public sealed record BackofficeAncillaryConnectivityDto(
        EnumValueDto PlanKind,
        int? DurationMinutes,
        int? IncludedDataMb,
        int? MaxDevices,
        EnumValueDto DeliveryStage,
        int? DeviceCount);

    public sealed record BackofficeAncillaryUpgradeDto(
        int FromCabinId,
        int ToCabinId,
        EnumValueDto UpgradeKind,
        bool RequiresTicketExchange);

    public sealed record BackofficeOrderPricingLineDto(
        long Id,
        EnumValueDto Reason,
        EnumValueDto Scope,
        EnumValueDto Category,
        EnumValueDto SubCategory,
        EnumValueDto Direction,
        EnumValueDto Treatment,
        string? Code,
        string? Description,
        string? Reference,
        decimal Amount,
        int CurrencyId,
        decimal EquivalentAmount,
        int EquivalentCurrencyId,
        EnumValueDto? Refundability,
        IReadOnlyList<OrderPricingAllocationDto> Allocations);

    public sealed record BackofficeOrderContactDto(
        long Id,
        int Sequence,
        EnumValueDto Role,
        string? ContactName,
        IReadOnlyList<BackofficeOrderContactPointDto> ContactPoints);

    public sealed record BackofficeOrderContactPointDto(
        long Id,
        EnumValueDto Type,
        string Value,
        string? CountryCode,
        bool IsPrimary);

    public sealed record BackofficeOrderRemarkDto(
        long Id,
        EnumValueDto Type,
        EnumValueDto Visibility,
        EnumValueDto Scope,
        long? TravellerId,
        long? SegmentId,
        long? OrderItemId,
        long? OrderServiceId,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice,
        EnumValueDto Status,
        long? SupersedesRemarkId,
        DateTimeOffset CreatedAt);

    public sealed record BackofficeElectronicTicketDto(
        long Id,
        long TravellerId,
        string DocumentNumber,
        EnumValueDto Authority,
        DateTimeOffset IssuedAt,
        decimal IssuedTotal,
        int CurrencyId,
        EnumValueDto StatusSummary,
        int DocumentVersion,
        DateTimeOffset? VoidDeadline,
        OrderDocumentVoidRecordDto? VoidRecord,
        IReadOnlyList<BackofficeTicketCouponDto> Coupons);

    public sealed record BackofficeOrderChangeDto(
        long Id,
        EnumValueDto ChangeType,
        int CommercialVersion,
        long ActorId,
        EnumValueDto Channel,
        EnumValueDto ContextType,
        EnumValueDto PrincipalType,
        string? SourceReference,
        string? SourceSystem,
        string? ReasonCode,
        string? ReasonText,
        bool IsInvoluntary,
        string? WaiverCode,
        DateTimeOffset CommittedAt);

    public sealed record BackofficeTicketCouponDto(
        long Id,
        int CouponNumber,
        long OriginalOrderServiceId,
        long CurrentOrderServiceId,
        long OrderSegmentId,
        string? FareBasisSnapshot,
        string? BookingClassSnapshot,
        long? RbdIdSnapshot,
        long? CabinClassIdSnapshot,
        decimal IssuanceValue,
        int CurrencyId,
        EnumValueDto FinancialStatus,
        EnumValueDto ControlStatus);
}
