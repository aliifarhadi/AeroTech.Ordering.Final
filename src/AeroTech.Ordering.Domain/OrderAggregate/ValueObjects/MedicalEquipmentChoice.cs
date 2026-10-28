using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class MedicalEquipmentChoice : ValueObject
    {
        private readonly List<string> _evidenceDocumentRefs = new();

        private MedicalEquipmentChoice()
        {
        }

        public MedicalEquipmentChoice(
            string equipmentCode,
            IReadOnlyCollection<string> evidenceDocumentRefs,
            decimal? oxygenUnits,
            decimal? oxygenFlowRateLitersPerMinute,
            string? frequentTravellerMedicalCardNumber)
        {
            EquipmentCode = equipmentCode;
            _evidenceDocumentRefs.AddRange(evidenceDocumentRefs);
            OxygenUnits = oxygenUnits;
            OxygenFlowRateLitersPerMinute = oxygenFlowRateLitersPerMinute;
            FrequentTravellerMedicalCardNumber = frequentTravellerMedicalCardNumber;
        }

        public string EquipmentCode { get; private set; } = default!;

        public IReadOnlyCollection<string> EvidenceDocumentRefs => _evidenceDocumentRefs.AsReadOnly();

        public decimal? OxygenUnits { get; private set; }

        public decimal? OxygenFlowRateLitersPerMinute { get; private set; }

        public string? FrequentTravellerMedicalCardNumber { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return EquipmentCode;
            yield return string.Join("|", _evidenceDocumentRefs);
            yield return OxygenUnits;
            yield return OxygenFlowRateLitersPerMinute;
            yield return FrequentTravellerMedicalCardNumber;
        }
    }
}
