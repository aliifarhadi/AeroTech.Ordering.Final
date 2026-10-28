using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using PassengerTypeCode = AeroTech.Messages.Ordering.Enums.PassengerTypeCode;

namespace AeroTech.Ordering.Query.OrderAggregate.Dto
{
    public sealed record OrderAncillaryContextDto(
        long OrderId,
        int CommercialVersion,
        bool AcceptsChanges,
        DateTimeOffset? TicketedAt,
        string SourceOfferId,
        int CurrencyId,
        long CustomerId,
        OrderAncillarySalesDto Sales,
        IReadOnlyList<OrderAncillaryTravellerDto> Travellers,
        IReadOnlyList<OrderAncillaryJourneyDto> Journeys,
        IReadOnlyList<OrderAncillaryAirServiceDto> AirServices,
        IReadOnlyList<OrderAncillaryServiceDto> AncillaryServices);

    public sealed record OrderAncillarySalesDto(SalesChannel Channel, long? TravelAgencyId);

    public sealed record OrderAncillaryTravellerDto(
        long TravellerId,
        PassengerTypeCode PassengerTypeCode,
        DateOnly? DateOfBirth,
        long? InfantParentTravellerId);

    public sealed record OrderAncillaryJourneyDto(
        string BoundId,
        int Sequence,
        int OriginAirportId,
        int DestinationAirportId,
        IReadOnlyList<OrderAncillarySegmentDto> Segments);

    public sealed record OrderAncillarySegmentDto(
        long SegmentId,
        int Sequence,
        long FlightId,
        int FlightVersion,
        string? FlightNumber,
        int OriginAirportId,
        int? OriginAirportTerminalId,
        int DestinationAirportId,
        int? DestinationAirportTerminalId,
        int OperatingAirlineId,
        int MarketingAirlineId,
        DateTimeOffset DepartureAt,
        DateTimeOffset ArrivalAt,
        int? AircraftId,
        IReadOnlyList<OrderAncillaryLegDto> Legs);

    public sealed record OrderAncillaryLegDto(
        long LegId,
        int Sequence,
        int OriginAirportId,
        int DestinationAirportId,
        DateTimeOffset DepartureAt,
        DateTimeOffset ArrivalAt);

    public sealed record OrderAncillaryAirServiceDto(
        long OrderServiceId,
        long TravellerId,
        long SegmentId,
        long FlightCapacityId,
        long? AirFareId,
        string? FareBasis,
        string? FareFamily,
        AirFareType? FareType,
        string? BookingClass,
        long? RbdId,
        long? CabinClassId,
        BaggageAllowanceDto? CheckedBaggageAllowance,
        BaggageAllowanceDto? CabinBaggageAllowance,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        bool? CouponOpenForUse);

    public sealed record OrderAncillaryServiceDto(
        long OrderServiceId,
        long? TravellerId,
        string? ServiceDefinitionRef,
        string VariantCode,
        AncillaryProfile Profile,
        SelectionScope? SelectionScope,
        ServiceCoverageScope? CoverageScope,
        IReadOnlyList<long> CoveredAirServiceIds,
        int Quantity,
        OrderServiceCommercialState CommercialState,
        long? SegmentId,
        string? SeatNumber);
}
