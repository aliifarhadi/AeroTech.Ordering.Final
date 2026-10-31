using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers.Ancillary.Wire;

namespace AeroTech.Ordering.Providers.Ancillary.Services
{
    public static class ResolvedSeatsMapper
    {
        private const int SingleSeat = 1;

        public static ResolvedSeatQuote ToDomain(string selectionId, ResolvedSeatsResponse source)
            => new(
                selectionId,
                source.SourceKind ?? throw Incomplete(selectionId, nameof(source.SourceKind)),
                source.SourceReference ?? throw Incomplete(selectionId, nameof(source.SourceReference)),
                source.OrderCommercialVersion,
                EarliestExpiryOf(selectionId, source),
                source.SelectedLineCount ?? throw Incomplete(selectionId, nameof(source.SelectedLineCount)),
                source.Seats.Select(seat => Seat(selectionId, seat)).ToList());

        private static DateTimeOffset EarliestExpiryOf(string selectionId, ResolvedSeatsResponse source)
            => source.Seats
                .Select(seat => seat.ExpiresAtUtc ?? throw Incomplete(selectionId, nameof(seat.ExpiresAtUtc)))
                .Append(source.ExpiresAtUtc ?? throw Incomplete(selectionId, nameof(source.ExpiresAtUtc)))
                .Min();

        private static ResolvedSeat Seat(string selectionId, ResolvedSeatLine seat)
        {
            var variantCode = seat.ServiceDefinition.VariantCode ?? throw Incomplete(selectionId, nameof(seat.ServiceDefinition.VariantCode));

            return new ResolvedSeat(
                seat.Id,
                variantCode,
                seat.TravellerRef,
                seat.FlightId,
                seat.SeatNumber,
                seat.PointOfSaleId,
                Definition(seat.ServiceDefinition, variantCode),
                Provision(selectionId, seat.Provision, seat.ServiceDefinition),
                Pricing(selectionId, seat.Pricing));
        }

        private static ResolvedAncillaryDefinition Definition(SeatDefinition definition, string variantCode)
            => new(
                definition.Id,
                definition.ServiceDefinitionRef,
                variantCode,
                definition.ServiceTypeCode,
                definition.ServiceSubCode,
                definition.Document.Rfic,
                definition.Document.Rfisc,
                definition.Booking.SsrCode);

        private static ResolvedAncillaryProvision Provision(string selectionId, SeatProvision provision, SeatDefinition definition)
        {
            var confirmation = Required(selectionId, definition.Booking.ConfirmationRequirement, nameof(definition.Booking.ConfirmationRequirement));

            return new ResolvedAncillaryProvision(
                provision.Id,
                Required(selectionId, provision.CoverageScope, nameof(provision.CoverageScope)),
                Required(selectionId, provision.PurchaseStage, nameof(provision.PurchaseStage)),
                Required(selectionId, definition.Booking.Method, nameof(definition.Booking.Method)),
                provision.Outcome.BookingRequired,
                confirmation,
                provision.Fulfillment.FulfillmentProviderKey,
                Required(selectionId, definition.Document.Routing, nameof(definition.Document.Routing)),
                Required(selectionId, definition.Document.Type, nameof(definition.Document.Type)),
                provision.Outcome.DocumentRequired,
                provision.Availability.MustCheckAvailability || confirmation == ConfirmationRequirement.SubjectToConfirmation);
        }

        private static ResolvedAncillaryPricing Pricing(string selectionId, SeatPricing pricing)
        {
            var rate = pricing.Rate;

            return new ResolvedAncillaryPricing(
                pricing.Id,
                rate.Id,
                pricing.Version,
                rate.CurrencyId,
                SingleSeat,
                rate.UnitTotal,
                rate.UnitTotal,
                Required(selectionId, pricing.PricingUnit, nameof(pricing.PricingUnit)),
                rate.BaseAmount,
                rate.IsUnitTotalComplete ?? throw Incomplete(selectionId, nameof(rate.IsUnitTotalComplete)),
                PriceLines(selectionId, rate, AncillaryPriceLineCategory.Tax),
                PriceLines(selectionId, rate, AncillaryPriceLineCategory.Fee));
        }

        private static IReadOnlyList<ResolvedAncillaryPriceLine> PriceLines(string selectionId, SeatRate rate, AncillaryPriceLineCategory category)
            => rate.Components
                .Where(component => Required(selectionId, component.Category, nameof(component.Category)) == category)
                .Select(component => new ResolvedAncillaryPriceLine(
                    component.Code,
                    component.Amount,
                    rate.CurrencyId,
                    component.CountryId,
                    component.StationAirportId,
                    Optional(selectionId, component.TaxTreatment, nameof(component.TaxTreatment)),
                    Optional(selectionId, component.FeeApplicationUnit, nameof(component.FeeApplicationUnit))))
                .ToList();

        private static TEnum Required<TEnum>(string selectionId, SeatEnum<TEnum>? value, string field)
            where TEnum : struct, Enum
            => Optional(selectionId, value, field) ?? throw Incomplete(selectionId, field);

        private static TEnum? Optional<TEnum>(string selectionId, SeatEnum<TEnum>? value, string field)
            where TEnum : struct, Enum
        {
            if (value is null)
                return null;

            return Enum.TryParse<TEnum>(value.Name, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw Incomplete(selectionId, field);
        }

        private static Exception Incomplete(string selectionId, string field)
            => ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, field);
    }
}
