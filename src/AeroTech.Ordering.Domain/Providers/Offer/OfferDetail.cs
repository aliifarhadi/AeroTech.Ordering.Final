using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.Providers.Offer
{
    public sealed record OfferDetail(
        string OfferId,
        int CurrencyId,
        DateTimeOffset? LastTicketingDate,
        IReadOnlyList<OfferTraveller> Travellers,
        IReadOnlyList<OfferBound> Bounds,
        IReadOnlyList<OfferPricingUnit> PricingUnits,
        IReadOnlyList<OfferTicket> Tickets,
        IReadOnlyList<OfferPriceLine> OrderCharges,
        IReadOnlyList<OfferRate> Rates);

    public sealed record OfferTraveller(string TravellerRef, int TravellerIndex, string PassengerTypeCode);

    public sealed record OfferBound(
        string BoundId,
        int Sequence,
        long OriginAirportId,
        long DestinationAirportId,
        IReadOnlyList<OfferFlight> Flights);

    public sealed record OfferFlight(
        int Sequence,
        long FlightId,
        int FlightVersion,
        string? FlightNumber,
        long OriginAirportId,
        long? OriginAirportTerminalId,
        long DestinationAirportId,
        long? DestinationAirportTerminalId,
        long OperatingAirlineId,
        long MarketingAirlineId,
        DateTimeOffset DepartureDateTime,
        DateTimeOffset ArrivalDateTime,
        int Duration,
        long? AircraftId,
        long? RbdId,
        long FlightCapacityId,
        IReadOnlyList<OfferFlightLeg> Legs);

    public sealed record OfferFlightLeg(
        int Sequence,
        long LegId,
        long OriginAirportId,
        long? OriginAirportTerminalId,
        long DestinationAirportId,
        long? DestinationAirportTerminalId,
        DateTimeOffset DepartureDateTime,
        DateTimeOffset ArrivalDateTime,
        OfferFlightStop? Stop);

    public sealed record OfferFlightStop(
        int DurationMinutes,
        StopType StopType,
        bool PassengersCanBoardOrLeave);

    public sealed record OfferPricingUnit(
        int Sequence,
        string SourceKind,
        FarePricingUnitType SemanticType,
        IReadOnlyList<string> CoveredBoundIds,
        IReadOnlyList<OfferFareComponent> FareComponents);

    public sealed record OfferFareComponent(
        int Sequence,
        string BoundId,
        long AirFareId,
        string? BookingClass,
        string? FareBasis,
        string? FareFamily,
        string? FareType);

    public sealed record OfferTicket(
        string TravellerRef,
        int TravellerIndex,
        IReadOnlyList<OfferCoupon> Coupons);

    public sealed record OfferCoupon(
        string BoundId,
        long FlightId,
        bool IsRefundable,
        bool IsChangeable,
        bool IsUpgradable,
        OfferBaggage? CheckedBaggage,
        OfferBaggage? CabinBaggage,
        IReadOnlyList<OfferPriceLine> PriceLines);

    public sealed record OfferBaggage(int Pieces, decimal Weight, string Unit);

    public sealed record OfferPriceLine(
        OfferPriceCategory Category,
        string? Name,
        string? Code,
        string? Reference,
        decimal Amount,
        int CurrencyId,
        decimal EquivalentAmount,
        int EquivalentCurrencyId,
        string? RateOfExchangePeriodId);

    public enum OfferPriceCategory
    {
        Fare = 0,
        Tax = 1,
        Fee = 2,
        Surcharge = 3
    }

    public sealed record OfferRate(
        string RateOfExchangePeriodId,
        int FromCurrencyId,
        int ToCurrencyId,
        decimal Rate,
        int DecimalPlaces);
}
