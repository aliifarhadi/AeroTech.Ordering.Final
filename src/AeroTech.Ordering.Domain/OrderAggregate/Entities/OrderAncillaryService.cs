using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public class OrderAncillaryService : OrderService
    {
        private readonly List<long> _coveredAirServiceIds = new();

        protected OrderAncillaryService()
        {
        }

        public OrderAncillaryService(
            long id,
            long orderId,
            long orderItemId,
            long? travellerId,
            AncillaryVariant variant,
            int quantity,
            SelectionScope selectionScope,
            ServiceCoverageScope coverageScope,
            IReadOnlyCollection<long> coveredAirServiceIds,
            AncillarySaleSource source,
            AncillaryDefinition definition,
            AncillaryProvision provision,
            AncillaryPricing pricing,
            MealChoice? meal,
            PetChoice? pet,
            AssistanceChoice? assistance,
            MedicalEquipmentChoice? medicalEquipment,
            UnaccompaniedMinorChoice? unaccompaniedMinor,
            AirportServiceChoice? airportService,
            PriorityChoice? priority,
            ConnectivityChoice? connectivity,
            UpgradeChoice? upgrade,
            ExtraSeatChoice? extraSeat,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : this(
                id,
                orderId,
                orderItemId,
                travellerId,
                variant,
                quantity,
                selectionScope,
                coverageScope,
                coveredAirServiceIds,
                source,
                definition,
                provision,
                pricing,
                createdByChangeId,
                createdAt)
        {
            Meal = meal;
            Pet = pet;
            Assistance = assistance;
            MedicalEquipment = medicalEquipment;
            UnaccompaniedMinor = unaccompaniedMinor;
            AirportService = airportService;
            Priority = priority;
            Connectivity = connectivity;
            Upgrade = upgrade;
            ExtraSeat = extraSeat;

            EnsureChoiceMatchesVariant(variant);
        }

        protected OrderAncillaryService(
            long id,
            long orderId,
            long orderItemId,
            long? travellerId,
            AncillaryVariant variant,
            int quantity,
            SelectionScope selectionScope,
            ServiceCoverageScope coverageScope,
            IReadOnlyCollection<long> coveredAirServiceIds,
            AncillarySaleSource source,
            AncillaryDefinition definition,
            AncillaryProvision provision,
            AncillaryPricing pricing,
            long createdByChangeId,
            DateTimeOffset createdAt)
            : base(id, orderId, orderItemId, travellerId, variant.ServiceType, FulfillmentProviderKeys.Ancillary, createdByChangeId, createdAt)
        {
            if (variant.IsSeatAssignment)
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(source.OfferItemId, variant.Code, variant.Profile);

            if (quantity <= 0)
                throw ExceptionFactory.AncillaryQuantityIsInvalid(source.OfferItemId, quantity, quantity);

            if (!ScopesAgree(selectionScope, coverageScope) || (travellerId is null) != (selectionScope == SelectionScope.PerOrder))
                throw ExceptionFactory.AncillaryScopeIsInconsistent(source.OfferItemId, selectionScope, coverageScope);

            if (coveredAirServiceIds.Count != coveredAirServiceIds.Distinct().Count() || coveredAirServiceIds.Any(airServiceId => airServiceId <= 0))
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(source.OfferItemId);

            Profile = variant.Profile;
            VariantCode = variant.Code;
            Quantity = quantity;
            SelectionScope = selectionScope;
            CoverageScope = coverageScope;
            _coveredAirServiceIds.AddRange(coveredAirServiceIds);
            Source = source;
            Definition = definition;
            Provision = provision;
            Pricing = pricing;
        }

        public AncillaryProfile Profile { get; private set; }

        public string VariantCode { get; private set; } = default!;

        public int Quantity { get; private set; }

        public SelectionScope SelectionScope { get; private set; }

        public ServiceCoverageScope CoverageScope { get; private set; }

        public IReadOnlyCollection<long> CoveredAirServiceIds => _coveredAirServiceIds.AsReadOnly();

        public AncillarySaleSource Source { get; private set; } = default!;

        public AncillaryDefinition Definition { get; private set; } = default!;

        public AncillaryProvision Provision { get; private set; } = default!;

        public AncillaryPricing Pricing { get; private set; } = default!;

        public MealChoice? Meal { get; private set; }

        public PetChoice? Pet { get; private set; }

        public AssistanceChoice? Assistance { get; private set; }

        public MedicalEquipmentChoice? MedicalEquipment { get; private set; }

        public UnaccompaniedMinorChoice? UnaccompaniedMinor { get; private set; }

        public AirportServiceChoice? AirportService { get; private set; }

        public PriorityChoice? Priority { get; private set; }

        public ConnectivityChoice? Connectivity { get; private set; }

        public UpgradeChoice? Upgrade { get; private set; }

        public ExtraSeatChoice? ExtraSeat { get; private set; }

        public static bool ScopesAgree(SelectionScope selectionScope, ServiceCoverageScope coverageScope)
            => (selectionScope, coverageScope) is
                (SelectionScope.PerOrder, ServiceCoverageScope.Order)
                or (SelectionScope.PerTraveller, ServiceCoverageScope.Journey)
                or (SelectionScope.PerTravellerPerPortion, ServiceCoverageScope.Portion)
                or (SelectionScope.PerTravellerPerFlight, ServiceCoverageScope.Sector);

        private void EnsureChoiceMatchesVariant(AncillaryVariant variant)
        {
            if (variant.Profile == AncillaryProfile.Baggage)
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(Source.OfferItemId, variant.Code, variant.Profile);

            (string Choice, bool IsExpected, bool IsPresent)[] choices =
            [
                (nameof(Meal), variant.Profile == AncillaryProfile.Meal, Meal is not null),
                (nameof(Pet), variant.Profile == AncillaryProfile.Pet, Pet is not null),
                (nameof(Assistance), variant.Profile == AncillaryProfile.AssistedTravel, Assistance is not null),
                (nameof(MedicalEquipment), variant.Code == AncillaryVariant.MedicalEquipment, MedicalEquipment is not null),
                (nameof(UnaccompaniedMinor), variant.Code == AncillaryVariant.UnaccompaniedMinor, UnaccompaniedMinor is not null),
                (nameof(AirportService), variant.Profile == AncillaryProfile.AirportService, AirportService is not null),
                (nameof(Priority), variant.Profile == AncillaryProfile.Priority, Priority is not null),
                (nameof(Connectivity), variant.Profile == AncillaryProfile.Connectivity, Connectivity is not null),
                (nameof(Upgrade), variant.Profile == AncillaryProfile.Upgrade, Upgrade is not null),
                (nameof(ExtraSeat), variant.Code == AncillaryVariant.ExtraSeat, ExtraSeat is not null)
            ];

            foreach (var (choice, isExpected, isPresent) in choices)
            {
                if (isExpected && !isPresent)
                    throw ExceptionFactory.AncillaryChoiceIsMissing(Source.OfferItemId, variant.Code, choice);

                if (!isExpected && isPresent)
                    throw ExceptionFactory.AncillaryChoiceIsInvalid(Source.OfferItemId, variant.Code, choice);
            }
        }
    }
}
