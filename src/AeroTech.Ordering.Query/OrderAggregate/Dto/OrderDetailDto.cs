using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain._Shared.Contracts;
using PassengerTypeCode = AeroTech.Messages.Ordering.Enums.PassengerTypeCode;

namespace AeroTech.Ordering.Query.OrderAggregate.Dto
{
    public sealed record OrderDetailDto(
        long Id,
        Guid OrderReference,
        string? RecordLocator,
        string SourceOfferId,
        OrderStatus Status,
        SalesChannel Channel,
        long CustomerId,
        SellingOfficeKind? OfficeKind,
        long? OfficeId,
        int CurrencyId,
        decimal CustomerTotal,
        int CommercialVersion,
        DateTimeOffset? LastTicketingDate,
        DateTimeOffset CreatedAt,
        IReadOnlyList<OrderTravellerDto> Travellers,
        IReadOnlyList<OrderJourneyDto> Journeys,
        IReadOnlyList<OrderItemDto> Items,
        IReadOnlyList<OrderPricingLineDto> PricingLines,
        IReadOnlyList<OrderContactDto> Contacts,
        IReadOnlyList<OrderRemarkDto> Remarks,
        IReadOnlyList<OrderElectronicTicketDto> Tickets,
        IReadOnlyList<OrderChangeDto> Changes);

    public sealed record OrderTravellerDto(
        long Id,
        int Index,
        PassengerTypeCode PassengerType,
        AgeRange AgeRange,
        OrderTravellerStatus Status,
        string GivenName,
        string? Surname,
        DateOnly? DateOfBirth,
        Gender? Gender,
        int? NationalityId,
        int? CountryOfResidenceId,
        long? InfantParentTravellerId,
        IReadOnlyList<OrderTravellerDocumentDto> Documents);

    public sealed record OrderTravellerDocumentDto(
        long Id,
        TravellerDocumentType Type,
        string Number,
        DateOnly? ExpiryDate,
        int IssuanceCountryId,
        string Holder);

    public sealed record OrderJourneyDto(
        long Id,
        int Sequence,
        string BoundId,
        int OriginAirportId,
        int DestinationAirportId,
        IReadOnlyList<OrderSegmentDto> Segments);

    public sealed record OrderSegmentDto(
        long Id,
        int Sequence,
        long FlightId,
        string? FlightNumber,
        int MarketingAirlineId,
        int OperatingAirlineId,
        int OriginAirportId,
        int DestinationAirportId,
        DateTimeOffset SoldDeparture,
        DateTimeOffset SoldArrival,
        int Duration);

    public sealed record OrderItemDto(
        long Id,
        ProductType Kind,
        decimal AcceptedTotal,
        OrderItemCommercialState CommercialStatus,
        long? EndedByChangeId,
        IReadOnlyList<OrderServiceDto> Services);

    public sealed record OrderServiceDto(
        long Id,
        long TravellerId,
        long SegmentId,
        OrderServiceType ServiceType,
        OrderServiceCommercialState CommercialStatus,
        string? BookingClass,
        string? FareBasis,
        string? FareFamily,
        string? FareType,
        BaggageAllowanceDto? CheckedBaggage,
        BaggageAllowanceDto? CabinBaggage,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        string? SeatNumber,
        long? EndedByChangeId);

    public sealed record BaggageAllowanceDto(int Pieces, decimal Weight, WeightUnit Unit);

    public sealed record OrderPricingLineDto(
        long Id,
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
        RefundabilityRule? Refundability,
        IReadOnlyList<OrderPricingAllocationDto> Allocations);

    public sealed record OrderPricingAllocationDto(
        long Id,
        long? OrderItemId,
        long? OrderServiceId,
        long? OrderJourneyId,
        long? OrderSegmentId,
        long? TravellerId,
        decimal Amount,
        int CurrencyId,
        decimal EquivalentAmount,
        int EquivalentCurrencyId);

    public sealed record OrderContactDto(
        long Id,
        int Sequence,
        ContactRole Role,
        string? ContactName,
        IReadOnlyList<OrderContactPointDto> ContactPoints);

    public sealed record OrderContactPointDto(
        long Id,
        ContactPointType Type,
        string Value,
        string? CountryCode,
        bool IsPrimary);

    public sealed record OrderRemarkDto(
        long Id,
        OrderRemarkType Type,
        OrderRemarkVisibility Visibility,
        OrderRemarkScope Scope,
        long? TravellerId,
        long? SegmentId,
        long? OrderItemId,
        long? OrderServiceId,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice,
        OrderRemarkStatus Status,
        long? SupersedesRemarkId,
        DateTimeOffset CreatedAt);

    public sealed record OrderElectronicTicketDto(
        long Id,
        long TravellerId,
        string DocumentNumber,
        DocumentAuthority Authority,
        DateTimeOffset IssuedAt,
        decimal IssuedTotal,
        int CurrencyId,
        ElectronicTicketStatus StatusSummary,
        int DocumentVersion,
        DateTimeOffset? VoidDeadline,
        OrderDocumentVoidRecordDto? VoidRecord,
        IReadOnlyList<OrderTicketCouponDto> Coupons);

    public sealed record OrderDocumentVoidRecordDto(
        long VoidFulfillmentTaskId,
        string? ReasonCode,
        string? ReasonText,
        string? ProviderReference,
        long? ActorId,
        DateTimeOffset VoidedAt);

    public sealed record OrderChangeDto(
        long Id,
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

    public sealed record OrderTicketCouponDto(
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
        TicketCouponFinancialStatus FinancialStatus,
        TicketCouponControlStatus ControlStatus);
}
