using System.Globalization;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public sealed record TravellerSpec(int Index, PassengerTypeCode PassengerType, AgeRange AgeRange, int? InfantParentIndex = null)
{
    public static TravellerSpec Adult(int index) => new(index, PassengerTypeCode.ADT, AgeRange.Adult);

    public static TravellerSpec Child(int index) => new(index, PassengerTypeCode.CHD, AgeRange.Child);

    public static TravellerSpec Infant(int index, int parentIndex) => new(index, PassengerTypeCode.INF, AgeRange.Infant, parentIndex);
}

public sealed record BoundSpec(string BoundId, params long[] FlightIds);

public sealed record SeatSpec(int TravellerIndex, string BoundId, string SeatNumber);

public sealed record FareSpec(long AirFareId, params long[] FlightIds);

public sealed record PricingUnitSpec(PricingUnitKind Kind, string[] BoundIds, params FareSpec[] FareComponents);

public static class OrderFixture
{
    public const long RbdId = 25;
    public const int AircraftId = 1;

    private const int CurrencyId = 1;
    private const long CapacityOffset = 90_000;

    public static long CapacityIdOf(long flightId) => flightId + CapacityOffset;

    public static string FlightIdOfCapacity(string capacityId)
        => (long.Parse(capacityId, CultureInfo.InvariantCulture) - CapacityOffset).ToString(CultureInfo.InvariantCulture);

    public static Order Create(
        IIdGenerator ids,
        IClock clock,
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<SeatSpec>? seats = null,
        DateTimeOffset? lastTicketingDate = null,
        IReadOnlyList<PricingUnitSpec>? pricingUnits = null)
        => Order.Create(
            Args(travellers, seats ?? []),
            Offer(clock, travellers, bounds, pricingUnits, lastTicketingDate),
            ids,
            clock);

    public static Order CreateFrom(OfferDetail offer, IIdGenerator ids, IClock clock, IReadOnlyList<TravellerSpec> travellers)
        => Order.Create(Args(travellers, []), offer, ids, clock);

    private static CreateOrderArgs Args(IReadOnlyList<TravellerSpec> travellers, IReadOnlyList<SeatSpec> seats)
        => new(
            "OFFER-1",
            42,
            new SalesContext(SalesChannel.BackOffice, CallerContextType.Airline, CallerPrincipalType.Human, 7, 7, null, null, null, null, null, null),
            new CreateOrderContactArgs("Contact", [new CreateOrderContactPointArgs(ContactPointType.Email, "contact@example.com", null, true)]),
            travellers.Select(ToArgs).ToList(),
            seats.Select(seat => new CreateOrderSeatSelectionArgs(seat.TravellerIndex, seat.BoundId, seat.SeatNumber)).ToList(),
            []);

    private static CreateOrderTravellerArgs ToArgs(TravellerSpec traveller)
        => new(
            traveller.Index,
            traveller.PassengerType,
            traveller.AgeRange,
            $"Given{traveller.Index}",
            $"Surname{traveller.Index}",
            false,
            traveller.AgeRange switch
            {
                AgeRange.Infant => new DateOnly(2026, 1, 1),
                AgeRange.Child => new DateOnly(2018, 1, 1),
                _ => new DateOnly(1990, 1, 1)
            },
            Gender.Male,
            null,
            null,
            traveller.InfantParentIndex,
            []);

    private static OfferDetail Offer(
        IClock clock,
        IReadOnlyList<TravellerSpec> travellers,
        IReadOnlyList<BoundSpec> bounds,
        IReadOnlyList<PricingUnitSpec>? pricingUnits,
        DateTimeOffset? lastTicketingDate)
    {
        var offerBounds = bounds.Select((bound, index) => OfferBound(clock, bound, index + 1)).ToList();
        var offerPricingUnits = pricingUnits ?? offerBounds.Select(OneWayPricingUnitOf).ToList();
        var offerFares = offerPricingUnits.SelectMany(unit => unit.FareComponents).ToList();

        return new OfferDetail(
            "OFFER-1",
            CurrencyId,
            lastTicketingDate,
            travellers.Select(traveller => new OfferTraveller(TravellerRef(traveller), traveller.Index, traveller.PassengerType.ToString())).ToList(),
            offerBounds,
            offerPricingUnits
                .Select((unit, unitIndex) => new OfferPricingUnit(
                    unitIndex + 1,
                    unit.Kind,
                    unit.BoundIds,
                    unit.FareComponents
                        .SelectMany(fare => BoundsPricedBy(fare, unit, offerBounds).Select(boundId => (fare.AirFareId, BoundId: boundId)))
                        .Select((occurrence, occurrenceIndex) => new OfferFareComponent(occurrenceIndex + 1, occurrence.BoundId, occurrence.AirFareId, "Y", "YOW", null, null))
                        .ToList()))
                .ToList(),
            travellers.Select(traveller => Ticket(traveller, offerBounds, offerFares)).ToList(),
            [],
            []);
    }

    private static IReadOnlyList<string> BoundsPricedBy(FareSpec fare, PricingUnitSpec unit, IReadOnlyList<OfferBound> bounds)
        => fare.FlightIds.Length == 0
            ? [unit.BoundIds[0]]
            : bounds
                .Where(bound => bound.Flights.Any(flight => fare.FlightIds.Contains(flight.FlightId)))
                .Select(bound => bound.BoundId)
                .ToList();

    private static PricingUnitSpec OneWayPricingUnitOf(OfferBound bound)
        => new(
            bound.Flights.Count > 1 ? PricingUnitKind.ThroughOneWay : PricingUnitKind.OneWay,
            [bound.BoundId],
            new FareSpec(7_000 + bound.Sequence, bound.Flights.Select(flight => flight.FlightId).ToArray()));

    public static int AirportOf(int boundSequence, int stop) => 10 * boundSequence + stop;

    private static OfferBound OfferBound(IClock clock, BoundSpec bound, int sequence)
        => new(
            bound.BoundId,
            sequence,
            AirportOf(sequence, 0),
            AirportOf(sequence, bound.FlightIds.Length),
            bound.FlightIds
                .Select((flightId, index) =>
                {
                    var departure = clock.GetDateTime().AddDays(10 + sequence).AddHours(index * 3);
                    return new OfferFlight(
                        index + 1,
                        flightId,
                        1,
                        $"DA{flightId}",
                        AirportOf(sequence, index),
                        null,
                        AirportOf(sequence, index + 1),
                        null,
                        10,
                        10,
                        departure,
                        departure.AddHours(1),
                        60,
                        AircraftId,
                        RbdId,
                        CapacityIdOf(flightId),
                        []);
                })
                .ToList());

    private static OfferTicket Ticket(TravellerSpec traveller, IReadOnlyList<OfferBound> bounds, IReadOnlyList<FareSpec> fares)
        => new(
            TravellerRef(traveller),
            traveller.Index,
            bounds
                .SelectMany(bound => bound.Flights.Select(flight => new OfferCoupon(
                    bound.BoundId,
                    flight.FlightId,
                    true,
                    true,
                    false,
                    null,
                    null,
                    [new OfferPriceLine(OfferPriceCategory.Fare, "Fare", "YOW", FareOf(fares, flight.FlightId).ToString(CultureInfo.InvariantCulture), 100m, CurrencyId, 100m, CurrencyId, null)])))
                .ToList());

    private static long FareOf(IReadOnlyList<FareSpec> fares, long flightId)
        => fares.Single(fare => fare.FlightIds.Contains(flightId)).AirFareId;

    private static string TravellerRef(TravellerSpec traveller) => $"T{traveller.Index}";
}
