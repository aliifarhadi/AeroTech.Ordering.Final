using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderPetService : OrderAncillaryService
    {
        private readonly List<string> _documentAcknowledgements = new();

        private OrderPetService()
        {
        }

        public OrderPetService(
            OrderAncillaryServiceArgs args,
            PetTransportMode transportMode,
            PetAnimalType? animalType,
            string? otherAnimalCode,
            int? animalAgeWeeks,
            decimal? combinedWeightKg,
            ServiceDimensions? carrierDimensions,
            IReadOnlyCollection<string> documentAcknowledgements,
            string? sizeBracket)
            : base(args, AncillaryProfile.Pet)
        {
            var expectedMode = VariantCode == AncillaryVariant.PetInHold ? PetTransportMode.Hold : PetTransportMode.Cabin;

            EnsureValid(transportMode == expectedMode, nameof(TransportMode));
            EnsureValid(animalAgeWeeks is null or >= 0, nameof(AnimalAgeWeeks));
            EnsureValid(combinedWeightKg is null or > 0, nameof(CombinedWeightKg));
            EnsureValid(IsPositive(carrierDimensions), nameof(CarrierDimensions));

            TransportMode = transportMode;
            AnimalType = animalType ?? throw MissingChoice(nameof(AnimalType));
            OtherAnimalCode = otherAnimalCode;
            AnimalAgeWeeks = animalAgeWeeks;
            CombinedWeightKg = combinedWeightKg ?? throw MissingChoice(nameof(CombinedWeightKg));
            CarrierDimensions = carrierDimensions ?? throw MissingChoice(nameof(CarrierDimensions));
            _documentAcknowledgements.AddRange(documentAcknowledgements);
            SizeBracket = sizeBracket;
        }

        public PetTransportMode TransportMode { get; private set; }

        public PetAnimalType AnimalType { get; private set; }

        public string? OtherAnimalCode { get; private set; }

        public int? AnimalAgeWeeks { get; private set; }

        public decimal CombinedWeightKg { get; private set; }

        public ServiceDimensions CarrierDimensions { get; private set; } = default!;

        public IReadOnlyCollection<string> DocumentAcknowledgements => _documentAcknowledgements.AsReadOnly();

        public string? SizeBracket { get; private set; }
    }
}
