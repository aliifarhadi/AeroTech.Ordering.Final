using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class BaggageChoice : ValueObject
    {
        private BaggageChoice()
        {
        }

        public BaggageChoice(
            BaggageChargeKind chargeKind,
            BaggageAllowanceConcept? allowanceConcept,
            decimal? packageWeightKg,
            decimal? weightFromExclusiveKg,
            decimal? weightToInclusiveKg,
            string? equipmentKind,
            string? bagRef,
            decimal? measuredWeightKg,
            ServiceDimensions? dimensions,
            decimal? weightKg)
        {
            ChargeKind = chargeKind;
            AllowanceConcept = allowanceConcept;
            PackageWeightKg = packageWeightKg;
            WeightFromExclusiveKg = weightFromExclusiveKg;
            WeightToInclusiveKg = weightToInclusiveKg;
            EquipmentKind = equipmentKind;
            BagRef = bagRef;
            MeasuredWeightKg = measuredWeightKg;
            Dimensions = dimensions;
            WeightKg = weightKg;
        }

        public BaggageChargeKind ChargeKind { get; private set; }

        public BaggageAllowanceConcept? AllowanceConcept { get; private set; }

        public decimal? PackageWeightKg { get; private set; }

        public decimal? WeightFromExclusiveKg { get; private set; }

        public decimal? WeightToInclusiveKg { get; private set; }

        public string? EquipmentKind { get; private set; }

        public string? BagRef { get; private set; }

        public decimal? MeasuredWeightKg { get; private set; }

        public ServiceDimensions? Dimensions { get; private set; }

        public decimal? WeightKg { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return ChargeKind;
            yield return AllowanceConcept;
            yield return PackageWeightKg;
            yield return WeightFromExclusiveKg;
            yield return WeightToInclusiveKg;
            yield return EquipmentKind;
            yield return BagRef;
            yield return MeasuredWeightKg;
            yield return Dimensions;
            yield return WeightKg;
        }
    }
}
