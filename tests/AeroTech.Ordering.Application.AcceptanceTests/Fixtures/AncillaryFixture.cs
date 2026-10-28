using System.Globalization;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.Ancillary;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public static class AncillaryFixture
{
    public const int CurrencyId = 1;

    public static readonly IReadOnlyList<string> GeneralVariants = AncillaryVariant.All
        .Where(variant => !variant.IsSeatAssignment)
        .Select(variant => variant.Code)
        .OrderBy(code => code, StringComparer.Ordinal)
        .ToList();

    public static ResolvedAncillarySelection Selection(Order order, DateTimeOffset now, params ResolvedAncillaryLine[] lines)
        => new(
            $"s_{order.Id}_{order.CommercialVersion}",
            ShoppingSourceKind.Order,
            order.Id.ToString(CultureInfo.InvariantCulture),
            order.CommercialVersion,
            now.AddMinutes(30),
            lines.Length,
            lines);

    public static ResolvedSeatQuote SeatQuote(Order order, DateTimeOffset now, params ResolvedSeat[] seats)
        => new(ShoppingSourceKind.Order, order.Id.ToString(CultureInfo.InvariantCulture), order.CommercialVersion, now.AddMinutes(30), seats);

    public static ResolvedSeat Seat(long travellerId, long flightId, string seatNumber, string variantCode = AncillaryVariant.StandardSeat, decimal price = 25m)
        => new(
            $"seat_{travellerId}_{flightId}_{seatNumber}",
            variantCode,
            travellerId.ToString(CultureInfo.InvariantCulture),
            flightId,
            seatNumber,
            PointOfSaleId: 7,
            Definition(variantCode),
            Provision(ServiceCoverageScope.Sector),
            Price(price));

    public static ResolvedAncillaryLine Sector(string variantCode, long travellerId, string flightRef, ResolvedAncillaryPricing? pricing = null)
        => Line(
            variantCode,
            SelectionScope.PerTravellerPerFlight,
            new ResolvedAncillaryUnit(Ref(travellerId), null, flightRef),
            new ResolvedAncillaryCoverage([Ref(travellerId)], [], [flightRef]),
            pricing);

    public static ResolvedAncillaryLine Portion(string variantCode, long travellerId, string portionRef, ResolvedAncillaryPricing? pricing = null)
        => Line(
            variantCode,
            SelectionScope.PerTravellerPerPortion,
            new ResolvedAncillaryUnit(Ref(travellerId), portionRef, null),
            new ResolvedAncillaryCoverage([Ref(travellerId)], [portionRef], []),
            pricing);

    public static ResolvedAncillaryLine Journey(string variantCode, long travellerId, IReadOnlyList<string> flightRefs, ResolvedAncillaryPricing? pricing = null)
        => Line(
            variantCode,
            SelectionScope.PerTraveller,
            new ResolvedAncillaryUnit(Ref(travellerId), null, null),
            new ResolvedAncillaryCoverage([Ref(travellerId)], [], flightRefs),
            pricing);

    public static ResolvedAncillaryLine WholeOrder(string variantCode, ResolvedAncillaryPricing? pricing = null)
        => Line(
            variantCode,
            SelectionScope.PerOrder,
            new ResolvedAncillaryUnit(null, null, null),
            new ResolvedAncillaryCoverage([], [], []),
            pricing);

    public static ResolvedAncillaryLine Line(
        string variantCode,
        SelectionScope selectionScope,
        ResolvedAncillaryUnit unit,
        ResolvedAncillaryCoverage coverage,
        ResolvedAncillaryPricing? pricing = null)
    {
        var variant = AncillaryVariant.Find(variantCode)!;
        var price = pricing ?? Price(10m);

        return new ResolvedAncillaryLine(
            $"d_{variantCode}_{unit.TravellerRef}_{unit.FlightRef ?? unit.PortionRef ?? "ORDER"}",
            variant.Profile,
            variantCode,
            selectionScope,
            unit,
            coverage,
            Definition(variantCode),
            Provision(CoverageOf(selectionScope)),
            price,
            price.Quantity,
            Details(variantCode));
    }

    public static ResolvedAncillaryPricing Price(decimal unitAmount, int quantity = 1)
        => new(11, 12, 1, CurrencyId, quantity, unitAmount, unitAmount * quantity, PricingUnit.PerItem, unitAmount, true, [], []);

    public static ResolvedAncillaryPricing Price(
        decimal baseAmount,
        IReadOnlyList<ResolvedAncillaryPriceLine> taxes,
        IReadOnlyList<ResolvedAncillaryPriceLine> fees,
        decimal unitAmount,
        int quantity = 1,
        bool isComplete = true,
        int currencyId = CurrencyId)
        => new(11, 12, 1, currencyId, quantity, unitAmount, unitAmount * quantity, PricingUnit.PerItem, baseAmount, isComplete, taxes, fees);

    public static ResolvedAncillaryPriceLine Tax(string code, decimal amount, TaxTreatment? treatment)
        => new(code, amount, CurrencyId, 364, null, treatment, null);

    public static ResolvedAncillaryPriceLine Fee(string code, decimal amount, FeeApplicationUnit? unit)
        => new(code, amount, CurrencyId, null, null, null, unit);

    public static ResolvedAncillaryDefinition Definition(string variantCode)
        => new(100 + int.Parse(variantCode[1..], CultureInfo.InvariantCulture), $"DEF_{variantCode}", variantCode, "F", "0CC", "C", "0CC", null);

    public static ResolvedAncillaryProvision Provision(ServiceCoverageScope coverageScope)
        => new(
            200,
            coverageScope,
            PurchaseStage.PreOrder,
            BookingMethod.Ssr,
            true,
            ConfirmationRequirement.Immediate,
            null,
            DocumentRouting.Emd,
            AncillaryDocumentType.EmdAssociated,
            true,
            false);

    public static ResolvedAncillaryDetails Details(string variantCode)
        => variantCode switch
        {
            AncillaryVariant.ExtraCheckedBag => new() { Baggage = Baggage(BaggageChargeKind.ExtraPiece) },
            AncillaryVariant.ExtraWeightPackage => new() { Baggage = Baggage(BaggageChargeKind.WeightPackage) with { PackageWeightKg = 10m } },
            AncillaryVariant.Overweight => new() { Baggage = Baggage(BaggageChargeKind.Overweight) with { BagRef = "BAG1", MeasuredWeightKg = 27.5m, WeightFromExclusiveKg = 23m, WeightToInclusiveKg = 32m } },
            AncillaryVariant.Oversize => new() { Baggage = Baggage(BaggageChargeKind.Oversize) with { Dimensions = new ResolvedDimensions(90m, 60m, 40m) } },
            AncillaryVariant.CabinBag => new() { Baggage = Baggage(BaggageChargeKind.ExtraPiece) },
            AncillaryVariant.SpecialEquipment => new() { Baggage = Baggage(BaggageChargeKind.SpecialEquipment) with { EquipmentKind = "GOLF", WeightKg = 15m } },
            AncillaryVariant.ExtraSeat => new() { ExtraSeat = new ResolvedExtraSeatDetails(SeatPurpose.ExtraSeat, ExtraSeatPurpose.PassengerComfort, 1, "12B") },
            AncillaryVariant.CabinUpgrade => new() { Upgrade = new ResolvedUpgradeDetails(1, 2, UpgradeKind.FixedAncillary, false) },
            AncillaryVariant.FreeSpecialMeal => new() { Meal = new ResolvedMealDetails(MealKind.SpecialRequest, "VGML", null, "VG", null) },
            AncillaryVariant.PaidPreorderMeal => new() { Meal = new ResolvedMealDetails(MealKind.PaidPreorder, null, "MENU_7", null, "MAIN") },
            AncillaryVariant.PetInCabin => new() { Pet = Pet(PetTransportMode.Cabin) },
            AncillaryVariant.PetInHold => new() { Pet = Pet(PetTransportMode.Hold) with { SizeBracket = "M" } },
            AncillaryVariant.Wheelchair => new() { Assistance = Assistance(AssistanceKind.Wheelchair) with { AssistanceSsrCode = "WCHR" } },
            AncillaryVariant.DisabilityAssistance => new() { Assistance = Assistance(AssistanceKind.DisabilityAssistance) with { AssistanceSsrCode = "BLND" } },
            AncillaryVariant.MedicalEquipment => new()
            {
                Assistance = Assistance(AssistanceKind.MedicalEquipment) with
                {
                    MedicalEquipment = new ResolvedMedicalEquipmentDetails("POC", ["MEDIF-1"], 2m, 3m, "FREMEC-9")
                }
            },
            AncillaryVariant.Bassinet => new() { Assistance = Assistance(AssistanceKind.Bassinet) },
            AncillaryVariant.UnaccompaniedMinor => new()
            {
                Assistance = Assistance(AssistanceKind.UnaccompaniedMinor) with
                {
                    UnaccompaniedMinor = new ResolvedUnaccompaniedMinorDetails("Parent A, +98 912 000 0000", "Uncle B, +44 20 0000 0000")
                }
            },
            AncillaryVariant.Lounge => new() { AirportService = AirportService(AirportServiceKind.Lounge) },
            AncillaryVariant.FastTrack => new() { AirportService = AirportService(AirportServiceKind.FastTrack) },
            AncillaryVariant.CipMeetAssist => new() { AirportService = AirportService(AirportServiceKind.Cip) },
            AncillaryVariant.PriorityBoardingCheckin => new() { Priority = new ResolvedPriorityDetails(PriorityKind.Boarding, "Z1", "G1", null, true) },
            AncillaryVariant.OnboardWifi => new() { Connectivity = new ResolvedConnectivityDetails(ConnectivityPlanKind.FullFlight, 600, 500, 2, PurchaseStage.PreOrder, 1) },
            _ => throw new ArgumentOutOfRangeException(nameof(variantCode), variantCode, null)
        };

    public static string FlightRef(string boundId, int sequence) => $"{boundId}:F{sequence}";

    private static string Ref(long travellerId) => travellerId.ToString(CultureInfo.InvariantCulture);

    private static ServiceCoverageScope CoverageOf(SelectionScope selectionScope)
        => selectionScope switch
        {
            SelectionScope.PerOrder => ServiceCoverageScope.Order,
            SelectionScope.PerTraveller => ServiceCoverageScope.Journey,
            SelectionScope.PerTravellerPerPortion => ServiceCoverageScope.Portion,
            _ => ServiceCoverageScope.Sector
        };

    private static ResolvedBaggageDetails Baggage(BaggageChargeKind chargeKind)
        => new(chargeKind, BaggageAllowanceConcept.Piece, null, null, null, null, null, null, null, null);

    private static ResolvedPetDetails Pet(PetTransportMode transportMode)
        => new(transportMode, PetAnimalType.Cat, null, 20, 6.5m, new ResolvedDimensions(45m, 30m, 25m), ["RABIES"], null);

    private static ResolvedAssistanceDetails Assistance(AssistanceKind kind)
        => new(kind, null, null, null, null, null);

    private static ResolvedAirportServiceDetails AirportService(AirportServiceKind kind)
        => new(kind, 2, "T1", 55, AirportServiceDirection.Departure, new DateTimeOffset(2026, 12, 27, 10, 0, 0, TimeSpan.FromHours(3.5)), 1);
}
