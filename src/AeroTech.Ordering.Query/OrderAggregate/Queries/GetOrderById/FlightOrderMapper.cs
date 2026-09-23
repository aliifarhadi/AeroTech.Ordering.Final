using System.Globalization;
using System.Xml;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public static class FlightOrderMapper
    {
        public static FlightOrderDto ToFlightOrder(OrderDetailDto order, ReferenceCodes codes)
        {
            var travellerIndexes = order.Travellers.ToDictionary(traveller => traveller.Id, traveller => traveller.Index);
            var customerLines = order.PricingLines.Where(line => line.Treatment == PricingLineTreatment.CustomerPrice).ToList();
            var currency = codes.Currency(order.CurrencyId);
            var services = order.Items.SelectMany(item => item.Services).ToList();

            return new FlightOrderDto(
                "flight-order",
                order.Id.ToString(CultureInfo.InvariantCulture),
                [new AssociatedRecordDto(order.OrderReference.ToString(), order.CreatedAt, order.SourceOfferId)],
                [ToFlightOffer(order, codes, customerLines, services, currency, travellerIndexes)],
                order.Travellers.Select(traveller => ToTraveler(traveller, codes, travellerIndexes)).ToList(),
                new FlightOrderRemarksDto(order.Remarks.Select(ToRemark).ToList()),
                order.Contacts.Select(ToContact).ToList());
        }

        private static FlightOfferDto ToFlightOffer(
            OrderDetailDto order,
            ReferenceCodes codes,
            IReadOnlyList<OrderPricingLineDto> customerLines,
            IReadOnlyList<OrderServiceDto> services,
            string? currency,
            IReadOnlyDictionary<long, int> travellerIndexes)
        {
            var airServices = services.Where(service => service.ServiceType == OrderServiceType.AirTransportation).ToList();

            return new FlightOfferDto(
                "flight-offer",
                order.SourceOfferId,
                order.LastTicketingDate,
                order.Journeys.Select(journey => ToItinerary(journey, codes)).ToList(),
                new FlightOfferPriceDto(
                    currency,
                    Money(SumOf(customerLines, OrderPricingLineCategory.Fare)),
                    Money(order.CustomerTotal),
                    Money(order.CustomerTotal)),
                new PricingOptionsDto(
                    airServices.Select(service => service.FareType).OfType<string>().Distinct().ToList(),
                    airServices.Count > 0 && airServices.All(service => service.CheckedBaggage is not null)),
                order.Travellers
                    .Select(traveller => ToTravelerPricing(traveller, services, customerLines, currency, travellerIndexes))
                    .ToList());
        }

        private static ItineraryDto ToItinerary(OrderJourneyDto journey, ReferenceCodes codes)
        {
            var duration = journey.Segments.Count == 0
                ? TimeSpan.Zero
                : journey.Segments[^1].SoldArrival - journey.Segments[0].SoldDeparture;

            return new ItineraryDto(
                journey.Id.ToString(CultureInfo.InvariantCulture),
                journey.BoundId,
                Duration(duration),
                journey.Segments.Select(segment => ToSegment(segment, codes)).ToList());
        }

        private static FlightSegmentDto ToSegment(OrderSegmentDto segment, ReferenceCodes codes)
            => new(
                segment.Id.ToString(CultureInfo.InvariantCulture),
                new FlightEndPointDto(codes.Airport(segment.OriginAirportId), segment.SoldDeparture),
                new FlightEndPointDto(codes.Airport(segment.DestinationAirportId), segment.SoldArrival),
                codes.Airline(segment.MarketingAirlineId),
                segment.FlightNumber,
                new OperatingFlightDto(codes.Airline(segment.OperatingAirlineId)),
                Duration(TimeSpan.FromMinutes(segment.Duration)),
                NumberOfStops: 0);

        private static TravelerPricingDto ToTravelerPricing(
            OrderTravellerDto traveller,
            IReadOnlyList<OrderServiceDto> services,
            IReadOnlyList<OrderPricingLineDto> customerLines,
            string? currency,
            IReadOnlyDictionary<long, int> travellerIndexes)
        {
            var travellerLines = customerLines
                .SelectMany(line => line.Allocations
                    .Where(allocation => allocation.TravellerId == traveller.Id)
                    .Select(allocation => (line, allocation)))
                .ToList();

            var seatsBySegment = services
                .Where(service => service.TravellerId == traveller.Id && service.ServiceType == OrderServiceType.SeatAssignment)
                .ToDictionary(service => service.SegmentId, service => service.SeatNumber);

            var taxes = travellerLines
                .Where(pair => pair.line.Category == OrderPricingLineCategory.Tax)
                .GroupBy(pair => pair.line.Code)
                .Select(group => new TaxDto(Money(group.Sum(pair => Signed(pair.line.Direction, pair.allocation.EquivalentAmount))), group.Key))
                .ToList();

            return new TravelerPricingDto(
                travellerIndexes[traveller.Id].ToString(CultureInfo.InvariantCulture),
                traveller.PassengerType,
                new TravelerPriceDto(
                    currency,
                    Money(travellerLines
                        .Where(pair => pair.line.Category == OrderPricingLineCategory.Fare)
                        .Sum(pair => Signed(pair.line.Direction, pair.allocation.EquivalentAmount))),
                    Money(travellerLines.Sum(pair => Signed(pair.line.Direction, pair.allocation.EquivalentAmount))),
                    taxes),
                services
                    .Where(service => service.TravellerId == traveller.Id && service.ServiceType == OrderServiceType.AirTransportation)
                    .Select(service => ToFareDetails(service, seatsBySegment))
                    .ToList());
        }

        private static FareDetailsBySegmentDto ToFareDetails(
            OrderServiceDto service,
            IReadOnlyDictionary<long, string?> seatsBySegment)
            => new(
                service.SegmentId.ToString(CultureInfo.InvariantCulture),
                service.BookingClass,
                service.FareBasis,
                service.FareFamily,
                service.FareType,
                service.CheckedBaggage,
                service.CabinBaggage,
                seatsBySegment.GetValueOrDefault(service.SegmentId),
                service.IsRefundable,
                service.IsChangeable,
                service.IsUpgradable);

        private static FlightOrderTravelerDto ToTraveler(
            OrderTravellerDto traveller,
            ReferenceCodes codes,
            IReadOnlyDictionary<long, int> travellerIndexes)
            => new(
                traveller.Index.ToString(CultureInfo.InvariantCulture),
                traveller.InfantParentTravellerId is { } parentId && travellerIndexes.TryGetValue(parentId, out var parentIndex)
                    ? parentIndex.ToString(CultureInfo.InvariantCulture)
                    : null,
                traveller.DateOfBirth,
                new TravelerNameDto(traveller.GivenName, traveller.Surname),
                traveller.Gender,
                codes.Country(traveller.NationalityId),
                codes.Country(traveller.CountryOfResidenceId),
                traveller.Documents.Select(document => ToDocument(document, codes)).ToList());

        private static TravelerDocumentDto ToDocument(OrderTravellerDocumentDto document, ReferenceCodes codes)
            => new(
                document.Type,
                document.Number,
                document.ExpiryDate,
                codes.Country(document.IssuanceCountryId),
                !string.IsNullOrEmpty(document.Holder));

        private static GeneralRemarkDto ToRemark(OrderRemarkDto remark)
            => new(
                remark.Id.ToString(CultureInfo.InvariantCulture),
                remark.Type,
                remark.Text,
                remark.Status,
                remark.SupersedesRemarkId?.ToString(CultureInfo.InvariantCulture));

        private static FlightOrderContactDto ToContact(OrderContactDto contact)
            => new(
                contact.ContactName,
                contact.ContactPoints.FirstOrDefault(point => point.Type == ContactPointType.Email)?.Value,
                contact.ContactPoints
                    .Where(point => point.Type != ContactPointType.Email)
                    .Select(point => new ContactPhoneDto(ToDeviceType(point.Type), point.CountryCode, point.Value))
                    .ToList());

        private static PhoneDeviceType ToDeviceType(ContactPointType type) => type switch
        {
            ContactPointType.Sms => PhoneDeviceType.SMS,
            ContactPointType.WhatsApp => PhoneDeviceType.WAP,
            ContactPointType.Telegram => PhoneDeviceType.TLG,
            _ => PhoneDeviceType.CAL
        };

        private static decimal SumOf(IReadOnlyList<OrderPricingLineDto> lines, OrderPricingLineCategory category)
            => lines
                .Where(line => line.Category == category)
                .Sum(line => Signed(line.Direction, line.EquivalentAmount));

        private static decimal Signed(OrderPricingLineDirection direction, decimal amount)
            => direction == OrderPricingLineDirection.Credit ? amount : -amount;

        private static string Money(decimal amount) => amount.ToString(CultureInfo.InvariantCulture);

        private static string Duration(TimeSpan duration) => XmlConvert.ToString(duration);
    }
}
