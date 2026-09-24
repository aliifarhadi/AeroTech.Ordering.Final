using System.Net;
using System.Text;
using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Providers.Offer.Services;
using AeroTech.Ordering.Providers.Pricing.Services;
using Xunit;
using PricingUnitKind = AeroTech.Messages.Ordering.Enums.PricingUnitKind;

namespace AeroTech.Ordering.Application.AcceptanceTests.Offer;

public sealed class RealOfferReservationValidationTests
{
    private const string OfferId = "2|0|0;1544009211417985689~1469435200688619520_1550116065948729631~1469436768104218624;3|1543368160932003840||70|1789762580";

    [Fact]
    public async Task Captured_round_trip_offer_with_one_way_units_is_validated_per_bound()
    {
        var clock = new FixedClock();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CapturedOffers.RawWithBoundIdentity(), Encoding.UTF8, "application/json")
        });

        var offer = await new OfferProvider(StubHttpMessageHandler.ClientFor(handler)).GetByOfferIdAsync(OfferId);
        var order = OrderFixture.CreateFrom(offer, new SequentialIdGenerator(), clock, [TravellerSpec.Adult(1), TravellerSpec.Adult(2)]);
        var pricing = new RecordingPricingProvider();

        await new AirFareReservationValidator(pricing, clock).ValidateAsync(order, ReservationHarness.AirServices(order).Select(service => service.Id).ToList());

        Assert.All(ReservationHarness.AirServices(order), service => Assert.Equal(25, service.RbdId));
        Assert.Equal(
            [(PricingUnitKind.OneWay, "B1", 1469435125056929792L, 2), (PricingUnitKind.OneWay, "B2", 1469436464520495104L, 2)],
            order.FarePricingUnits.Select(unit => (
                unit.Kind,
                order.Journeys.Single(journey => journey.Id == unit.CoveredJourneyIds.Single()).BoundId,
                unit.FareComponents.Single().AirFareId,
                unit.FareComponents.Single().CoveredOrderServiceIds.Count)));
        Assert.Equal(
            [
                (JourneyType.OneWay, 2, 6, "B1", "1469435125056929792", "1544009211417985664", "1597", 1, 25L, 2),
                (JourneyType.OneWay, 6, 2, "B2", "1469436464520495104", "1550116065948729606", "1598", 1, 25L, 2)
            ],
            pricing.Requests.Single().PricingUnits.Select(unit =>
            {
                var fare = unit.AirFares.Single();
                var flight = fare.Flights.Single();

                return (
                    unit.JourneyType,
                    unit.PricingOriginAirportId,
                    unit.PricingDestinationAirportId,
                    unit.BoundIds.Single(),
                    fare.AirFareId,
                    flight.FlightId,
                    flight.FlightNumber,
                    flight.AircraftId,
                    flight.RbdId,
                    flight.RequiredSeats);
            }));
    }
}
