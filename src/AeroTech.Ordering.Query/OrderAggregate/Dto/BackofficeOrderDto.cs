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
        IReadOnlyList<BackofficeOrderRemarkDto> Remarks);

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
        IReadOnlyList<BackofficeOrderServiceDto> Services);

    public sealed record BackofficeOrderServiceDto(
        long Id,
        long TravellerId,
        long SegmentId,
        EnumValueDto ServiceType,
        EnumValueDto CommercialStatus,
        string? BookingClass,
        string? FareBasis,
        string? FareFamily,
        string? FareType,
        BackofficeBaggageAllowanceDto? CheckedBaggage,
        BackofficeBaggageAllowanceDto? CabinBaggage,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        string? SeatNumber);

    public sealed record BackofficeBaggageAllowanceDto(int Pieces, decimal Weight, EnumValueDto Unit);

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
}
