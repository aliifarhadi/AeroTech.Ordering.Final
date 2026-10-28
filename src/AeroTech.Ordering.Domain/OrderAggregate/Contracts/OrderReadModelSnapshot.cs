using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Contracts;
using PassengerTypeCode = AeroTech.Messages.Ordering.Enums.PassengerTypeCode;

namespace AeroTech.Ordering.Domain.OrderAggregate.Contracts
{
    public sealed record OrderReadModelSnapshot(
        long OrderId,
        Guid OrderReference,
        string? RecordLocator,
        string SourceOfferId,
        OrderStatus Status,
        SalesChannel Channel,
        long CustomerId,
        long ActorId,
        long? TravelAgencyId,
        SellingOfficeKind? OfficeKind,
        long? OfficeId,
        int CurrencyId,
        decimal CustomerTotal,
        int CommercialVersion,
        DateTimeOffset? LastTicketingDate,
        DateTimeOffset CreatedAt,
        DateTimeOffset OccurredAt)
    {
        public IReadOnlyList<OrderTravellerSnapshot> Travellers { get; init; } = Array.Empty<OrderTravellerSnapshot>();

        public IReadOnlyList<OrderJourneySnapshot> Journeys { get; init; } = Array.Empty<OrderJourneySnapshot>();

        public IReadOnlyList<OrderSegmentSnapshot> Segments { get; init; } = Array.Empty<OrderSegmentSnapshot>();

        public IReadOnlyList<OrderItemSnapshot> Items { get; init; } = Array.Empty<OrderItemSnapshot>();

        public IReadOnlyList<OrderServiceSnapshot> Services { get; init; } = Array.Empty<OrderServiceSnapshot>();

        public IReadOnlyList<OrderPricingLineSnapshot> PricingLines { get; init; } = Array.Empty<OrderPricingLineSnapshot>();

        public IReadOnlyList<OrderContactSnapshot> Contacts { get; init; } = Array.Empty<OrderContactSnapshot>();

        public IReadOnlyList<OrderRemarkSnapshot> Remarks { get; init; } = Array.Empty<OrderRemarkSnapshot>();

        public IReadOnlyList<OrderChangeSnapshot> Changes { get; init; } = Array.Empty<OrderChangeSnapshot>();
    }

    public sealed record OrderTravellerSnapshot(
        long TravellerId,
        int Index,
        string? SourceTravellerRef,
        PassengerTypeCode PassengerType,
        AgeRange AgeRange,
        long? InfantParentTravellerId,
        OrderTravellerStatus Status,
        string GivenName,
        string? Surname,
        DateOnly? DateOfBirth,
        Gender? Gender,
        int? NationalityId,
        int? CountryOfResidenceId)
    {
        public IReadOnlyList<OrderTravellerDocumentSnapshot> Documents { get; init; } = Array.Empty<OrderTravellerDocumentSnapshot>();
    }

    public sealed record OrderTravellerDocumentSnapshot(
        long DocumentId,
        TravellerDocumentType Type,
        string Number,
        DateOnly? ExpiryDate,
        int IssuanceCountryId,
        string Holder);

    public sealed record OrderJourneySnapshot(
        long JourneyId,
        int Sequence,
        string BoundId,
        int OriginAirportId,
        int DestinationAirportId);

    public sealed record OrderSegmentSnapshot(
        long SegmentId,
        long JourneyId,
        int Sequence,
        long FlightId,
        int FlightVersion,
        string? FlightNumber,
        int MarketingAirlineId,
        int OperatingAirlineId,
        int OriginAirportId,
        int? OriginAirportTerminalId,
        int DestinationAirportId,
        int? DestinationAirportTerminalId,
        DateTimeOffset SoldDeparture,
        DateTimeOffset SoldArrival,
        int Duration,
        int? AircraftId)
    {
        public IReadOnlyList<OrderSegmentLegSnapshot> Legs { get; init; } = Array.Empty<OrderSegmentLegSnapshot>();
    }

    public sealed record OrderSegmentLegSnapshot(
        long SegmentLegId,
        int Sequence,
        long LegId,
        int OriginAirportId,
        int DestinationAirportId,
        DateTimeOffset DepartureDateTime,
        DateTimeOffset ArrivalDateTime);

    public sealed record OrderItemSnapshot(
        long ItemId,
        ProductType Kind,
        decimal AcceptedTotal,
        OrderItemCommercialState CommercialStatus,
        long? EndedByChangeId);

    public sealed record OrderServiceSnapshot(
        long ServiceId,
        long ItemId,
        long? TravellerId,
        long? SegmentId,
        OrderServiceType ServiceType,
        OrderServiceCommercialState CommercialStatus,
        long? FlightCapacityId,
        long? AirFareId,
        string? BookingClass,
        string? FareBasis,
        string? FareFamily,
        AirFareType? FareType,
        long? RbdId,
        long? CabinClassId,
        BaggageAllowanceSnapshot? CheckedBaggage,
        BaggageAllowanceSnapshot? CabinBaggage,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        string? SeatNumber,
        long? EndedByChangeId)
    {
        public OrderAncillarySnapshot? Ancillary { get; init; }
    }

    public sealed record OrderAncillarySnapshot(
        string VariantCode,
        AncillaryProfile Profile,
        int Quantity,
        SelectionScope? SelectionScope,
        ServiceCoverageScope? CoverageScope,
        IReadOnlyList<long> CoveredAirServiceIds,
        long? PointOfSaleId,
        AncillarySaleSource Source,
        AncillaryDefinition Definition,
        AncillaryProvision Provision,
        AncillaryPricing Pricing)
    {
        public BaggageChoice? Baggage { get; init; }

        public MealChoice? Meal { get; init; }

        public PetChoice? Pet { get; init; }

        public AssistanceChoice? Assistance { get; init; }

        public AirportServiceChoice? AirportService { get; init; }

        public PriorityChoice? Priority { get; init; }

        public ConnectivityChoice? Connectivity { get; init; }

        public UpgradeChoice? Upgrade { get; init; }
    }

    public sealed record OrderChangeSnapshot(
        long ChangeId,
        OrderChangeType ChangeType,
        int CommercialVersion,
        long ActorId,
        SalesChannel Channel,
        CallerContextType ContextType,
        CallerPrincipalType PrincipalType,
        string? SourceReference,
        string? SourceSystem,
        string? ReasonCode,
        string? ReasonText,
        bool IsInvoluntary,
        string? WaiverCode,
        DateTimeOffset CommittedAt);

    public sealed record BaggageAllowanceSnapshot(int Pieces, decimal Weight, WeightUnit Unit);

    public sealed record OrderPricingLineSnapshot(
        long PricingLineId,
        OrderPricingReason Reason,
        PricingLineScope Scope,
        OrderPricingLineCategory Category,
        OrderPricingLineSubCategory SubCategory,
        OrderPricingLineDirection Direction,
        PricingLineTreatment Treatment,
        string? Code,
        string? Description,
        string? Reference,
        decimal Amount,
        int CurrencyId,
        decimal EquivalentAmount,
        int EquivalentCurrencyId,
        RefundabilityRule? Refundability)
    {
        public IReadOnlyList<OrderPricingAllocationSnapshot> Allocations { get; init; } = Array.Empty<OrderPricingAllocationSnapshot>();
    }

    public sealed record OrderPricingAllocationSnapshot(
        long AllocationId,
        long? ItemId,
        long? ServiceId,
        long? JourneyId,
        long? SegmentId,
        long? TravellerId,
        decimal Amount,
        int CurrencyId,
        decimal EquivalentAmount,
        int EquivalentCurrencyId);

    public sealed record OrderContactSnapshot(
        long ContactId,
        int Sequence,
        ContactRole Role,
        string? ContactName)
    {
        public IReadOnlyList<OrderContactPointSnapshot> ContactPoints { get; init; } = Array.Empty<OrderContactPointSnapshot>();
    }

    public sealed record OrderContactPointSnapshot(
        long ContactPointId,
        ContactPointType Type,
        string Value,
        string? CountryCode,
        bool IsPrimary);

    public sealed record OrderRemarkSnapshot(
        long RemarkId,
        OrderRemarkType Type,
        OrderRemarkVisibility Visibility,
        OrderRemarkScope Scope,
        long? TravellerId,
        long? SegmentId,
        long? ItemId,
        long? ServiceId,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice,
        OrderRemarkStatus Status,
        long? SupersedesRemarkId,
        long CreatedBy,
        DateTimeOffset CreatedAt);
}
