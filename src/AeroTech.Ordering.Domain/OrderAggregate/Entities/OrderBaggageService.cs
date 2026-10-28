using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderBaggageService : OrderAncillaryService
    {
        private OrderBaggageService()
        {
        }

        public OrderBaggageService(
            OrderAncillaryServiceArgs args,
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
            : base(args, AncillaryProfile.Baggage)
        {
            EnsurePresent(VariantCode != AncillaryVariant.Overweight || !string.IsNullOrWhiteSpace(bagRef), nameof(BagRef));
            EnsurePresent(VariantCode != AncillaryVariant.Overweight || measuredWeightKg is not null, nameof(MeasuredWeightKg));
            EnsurePresent(VariantCode != AncillaryVariant.Oversize || dimensions is not null, nameof(Dimensions));
            EnsurePresent(VariantCode != AncillaryVariant.SpecialEquipment || !string.IsNullOrWhiteSpace(equipmentKind), nameof(EquipmentKind));
            EnsureValid(packageWeightKg is null or > 0, nameof(PackageWeightKg));
            EnsureValid(measuredWeightKg is null or > 0, nameof(MeasuredWeightKg));
            EnsureValid(weightKg is null or > 0, nameof(WeightKg));
            EnsureValid(IsPositive(dimensions), nameof(Dimensions));

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
    }
}
