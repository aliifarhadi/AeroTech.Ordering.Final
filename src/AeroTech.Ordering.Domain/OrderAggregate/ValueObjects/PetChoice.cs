using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class PetChoice : ValueObject
    {
        private readonly List<string> _documentAcknowledgements = new();

        private PetChoice()
        {
        }

        public PetChoice(
            PetTransportMode transportMode,
            PetAnimalType animalType,
            string? otherAnimalCode,
            int? animalAgeWeeks,
            decimal combinedWeightKg,
            ServiceDimensions carrierDimensions,
            IReadOnlyCollection<string> documentAcknowledgements,
            string? sizeBracket)
        {
            TransportMode = transportMode;
            AnimalType = animalType;
            OtherAnimalCode = otherAnimalCode;
            AnimalAgeWeeks = animalAgeWeeks;
            CombinedWeightKg = combinedWeightKg;
            CarrierDimensions = carrierDimensions;
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

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return TransportMode;
            yield return AnimalType;
            yield return OtherAnimalCode;
            yield return AnimalAgeWeeks;
            yield return CombinedWeightKg;
            yield return CarrierDimensions;
            yield return string.Join("|", _documentAcknowledgements);
            yield return SizeBracket;
        }
    }
}
