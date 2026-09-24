using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
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
        IReadOnlyList<OrderRemarkDto> Remarks);

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
        string? SeatNumber);

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
}
