using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed record AncillaryVariant(
        string Code,
        AncillaryProfile Profile,
        OrderServiceType ServiceType,
        ProductType ProductType,
        bool IsSeatAssignment)
    {
        public const string ExtraCheckedBag = "A01";
        public const string ExtraWeightPackage = "A02";
        public const string Overweight = "A03";
        public const string Oversize = "A04";
        public const string CabinBag = "A05";
        public const string SpecialEquipment = "A06";
        public const string StandardSeat = "A07";
        public const string PreferredSeat = "A08";
        public const string ExtraSeat = "A09";
        public const string CabinUpgrade = "A10";
        public const string FreeSpecialMeal = "A11";
        public const string PaidPreorderMeal = "A12";
        public const string PetInCabin = "A13";
        public const string PetInHold = "A14";
        public const string Wheelchair = "A15";
        public const string DisabilityAssistance = "A16";
        public const string MedicalEquipment = "A17";
        public const string Bassinet = "A18";
        public const string UnaccompaniedMinor = "A19";
        public const string Lounge = "A20";
        public const string FastTrack = "A21";
        public const string CipMeetAssist = "A22";
        public const string PriorityBoardingCheckin = "A23";
        public const string OnboardWifi = "A24";

        private static readonly IReadOnlyDictionary<string, AncillaryVariant> Registry = new AncillaryVariant[]
        {
            new(ExtraCheckedBag, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(ExtraWeightPackage, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(Overweight, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(Oversize, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(CabinBag, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(SpecialEquipment, AncillaryProfile.Baggage, OrderServiceType.Ancillary, ProductType.Baggage, false),
            new(StandardSeat, AncillaryProfile.Seat, OrderServiceType.SeatAssignment, ProductType.Seat, true),
            new(PreferredSeat, AncillaryProfile.Seat, OrderServiceType.SeatAssignment, ProductType.Seat, true),
            new(ExtraSeat, AncillaryProfile.Seat, OrderServiceType.ExtraSeat, ProductType.Seat, false),
            new(CabinUpgrade, AncillaryProfile.Upgrade, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(FreeSpecialMeal, AncillaryProfile.Meal, OrderServiceType.Meal, ProductType.Meal, false),
            new(PaidPreorderMeal, AncillaryProfile.Meal, OrderServiceType.Meal, ProductType.Meal, false),
            new(PetInCabin, AncillaryProfile.Pet, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(PetInHold, AncillaryProfile.Pet, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(Wheelchair, AncillaryProfile.AssistedTravel, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(DisabilityAssistance, AncillaryProfile.AssistedTravel, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(MedicalEquipment, AncillaryProfile.AssistedTravel, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(Bassinet, AncillaryProfile.AssistedTravel, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(UnaccompaniedMinor, AncillaryProfile.AssistedTravel, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(Lounge, AncillaryProfile.AirportService, OrderServiceType.LoungeAccess, ProductType.Ancillary, false),
            new(FastTrack, AncillaryProfile.AirportService, OrderServiceType.Ancillary, ProductType.Ancillary, false),
            new(CipMeetAssist, AncillaryProfile.AirportService, OrderServiceType.Cip, ProductType.Ancillary, false),
            new(PriorityBoardingCheckin, AncillaryProfile.Priority, OrderServiceType.Priority, ProductType.Ancillary, false),
            new(OnboardWifi, AncillaryProfile.Connectivity, OrderServiceType.WiFi, ProductType.Ancillary, false)
        }.ToDictionary(variant => variant.Code, StringComparer.Ordinal);

        public static IReadOnlyCollection<AncillaryVariant> All => Registry.Values.ToList();

        public static AncillaryVariant? Find(string? code)
            => code is not null && Registry.TryGetValue(code, out var variant) ? variant : null;
    }
}
