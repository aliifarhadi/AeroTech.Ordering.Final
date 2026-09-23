using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Dto
{
    public sealed record FlightOrderDto(
        string Type,
        string Id,
        IReadOnlyList<AssociatedRecordDto> AssociatedRecords,
        IReadOnlyList<FlightOfferDto> FlightOffers,
        IReadOnlyList<FlightOrderTravelerDto> Travelers,
        FlightOrderRemarksDto Remarks,
        IReadOnlyList<FlightOrderContactDto> Contacts);

    public sealed record AssociatedRecordDto(
        string Reference,
        DateTimeOffset CreationDate,
        string FlightOfferId);

    public sealed record FlightOfferDto(
        string Type,
        string Id,
        DateTimeOffset? LastTicketingDate,
        IReadOnlyList<ItineraryDto> Itineraries,
        FlightOfferPriceDto Price,
        PricingOptionsDto PricingOptions,
        IReadOnlyList<TravelerPricingDto> TravelerPricings);

    public sealed record ItineraryDto(
        string Id,
        string BoundId,
        string Duration,
        IReadOnlyList<FlightSegmentDto> Segments);

    public sealed record FlightSegmentDto(
        string Id,
        FlightEndPointDto Departure,
        FlightEndPointDto Arrival,
        string? CarrierCode,
        string? Number,
        OperatingFlightDto Operating,
        string Duration,
        int NumberOfStops);

    public sealed record FlightEndPointDto(string? IataCode, DateTimeOffset At);

    public sealed record OperatingFlightDto(string? CarrierCode);

    public sealed record FlightOfferPriceDto(
        string? Currency,
        string Base,
        string Total,
        string GrandTotal);

    public sealed record PricingOptionsDto(
        IReadOnlyList<string> FareType,
        bool IncludedCheckedBagsOnly);

    public sealed record TravelerPricingDto(
        string TravelerId,
        PassengerTypeCode TravelerType,
        TravelerPriceDto Price,
        IReadOnlyList<FareDetailsBySegmentDto> FareDetailsBySegment);

    public sealed record TravelerPriceDto(
        string? Currency,
        string Base,
        string Total,
        IReadOnlyList<TaxDto> Taxes);

    public sealed record TaxDto(string Amount, string? Code);

    public sealed record FareDetailsBySegmentDto(
        string SegmentId,
        string? Class,
        string? FareBasis,
        string? BrandedFare,
        string? FareType,
        BaggageAllowanceDto? IncludedCheckedBags,
        BaggageAllowanceDto? IncludedCabinBags,
        string? SeatNumber,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable);

    public sealed record FlightOrderTravelerDto(
        string Id,
        string? AssociatedAdultId,
        DateOnly? DateOfBirth,
        TravelerNameDto Name,
        Gender? Gender,
        string? Nationality,
        string? CountryOfResidence,
        IReadOnlyList<TravelerDocumentDto> Documents);

    public sealed record TravelerNameDto(string FirstName, string? LastName);

    public sealed record TravelerDocumentDto(
        TravellerDocumentType DocumentType,
        string Number,
        DateOnly? ExpiryDate,
        string? IssuanceCountry,
        bool Holder);

    public sealed record FlightOrderRemarksDto(IReadOnlyList<GeneralRemarkDto> General);

    public sealed record GeneralRemarkDto(
        string Id,
        OrderRemarkType SubType,
        string Text,
        OrderRemarkStatus Status,
        string? SupersedesRemarkId);

    public sealed record FlightOrderContactDto(
        string? AddresseeName,
        string? EmailAddress,
        IReadOnlyList<ContactPhoneDto> Phones);

    public sealed record ContactPhoneDto(
        PhoneDeviceType DeviceType,
        string? CountryCallingCode,
        string Number);
}
