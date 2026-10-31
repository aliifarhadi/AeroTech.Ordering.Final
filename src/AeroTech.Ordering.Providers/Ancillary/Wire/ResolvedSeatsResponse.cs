using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Providers.Ancillary.Wire
{
    public sealed class ResolvedSeatsResponse
    {
        public ShoppingSourceKind? SourceKind { get; set; }

        public string? SourceReference { get; set; }

        public int? OrderCommercialVersion { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public int? SelectedLineCount { get; set; }

        public List<ResolvedSeatLine> Seats { get; set; } = new();
    }

    public sealed class ResolvedSeatLine
    {
        public string Id { get; set; } = string.Empty;

        public string TravellerRef { get; set; } = string.Empty;

        public long FlightId { get; set; }

        public string SeatNumber { get; set; } = string.Empty;

        public long PointOfSaleId { get; set; }

        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public SeatDefinition ServiceDefinition { get; set; } = new();

        public SeatProvision Provision { get; set; } = new();

        public SeatPricing Pricing { get; set; } = new();
    }

    public sealed class SeatEnum<TEnum>
        where TEnum : struct, Enum
    {
        public string? Name { get; set; }
    }

    public sealed class SeatDefinition
    {
        public long Id { get; set; }

        public string ServiceDefinitionRef { get; set; } = string.Empty;

        public string? VariantCode { get; set; }

        public string ServiceTypeCode { get; set; } = string.Empty;

        public string ServiceSubCode { get; set; } = string.Empty;

        public SeatBooking Booking { get; set; } = new();

        public SeatDocument Document { get; set; } = new();
    }

    public sealed class SeatBooking
    {
        public SeatEnum<BookingMethod>? Method { get; set; }

        public string? SsrCode { get; set; }

        public SeatEnum<ConfirmationRequirement>? ConfirmationRequirement { get; set; }
    }

    public sealed class SeatDocument
    {
        public SeatEnum<AncillaryDocumentType>? Type { get; set; }

        public SeatEnum<DocumentRouting>? Routing { get; set; }

        public string? Rfic { get; set; }

        public string? Rfisc { get; set; }
    }

    public sealed class SeatProvision
    {
        public long Id { get; set; }

        public SeatEnum<ServiceCoverageScope>? CoverageScope { get; set; }

        public SeatEnum<PurchaseStage>? PurchaseStage { get; set; }

        public SeatOutcome Outcome { get; set; } = new();

        public SeatAvailability Availability { get; set; } = new();

        public SeatFulfillment Fulfillment { get; set; } = new();
    }

    public sealed class SeatOutcome
    {
        public bool DocumentRequired { get; set; }

        public bool BookingRequired { get; set; }
    }

    public sealed class SeatAvailability
    {
        public bool MustCheckAvailability { get; set; }
    }

    public sealed class SeatFulfillment
    {
        public string? FulfillmentProviderKey { get; set; }
    }

    public sealed class SeatPricing
    {
        public long Id { get; set; }

        public int Version { get; set; }

        public SeatEnum<PricingUnit>? PricingUnit { get; set; }

        public SeatRate Rate { get; set; } = new();
    }

    public sealed class SeatRate
    {
        public long Id { get; set; }

        public int CurrencyId { get; set; }

        public decimal BaseAmount { get; set; }

        public decimal UnitTotal { get; set; }

        public bool? IsUnitTotalComplete { get; set; }

        public List<SeatPriceComponent> Components { get; set; } = new();
    }

    public sealed class SeatPriceComponent
    {
        public SeatEnum<AncillaryPriceLineCategory>? Category { get; set; }

        public string? Code { get; set; }

        public int? CountryId { get; set; }

        public int? StationAirportId { get; set; }

        public decimal Amount { get; set; }

        public SeatEnum<FeeApplicationUnit>? FeeApplicationUnit { get; set; }

        public SeatEnum<TaxTreatment>? TaxTreatment { get; set; }
    }
}
