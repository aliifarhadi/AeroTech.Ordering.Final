using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderAssistedTravelService : OrderAncillaryService
    {
        private OrderAssistedTravelService()
        {
        }

        public OrderAssistedTravelService(
            OrderAncillaryServiceArgs args,
            AssistanceKind assistanceKind,
            string? assistanceSsrCode,
            long? infantTravellerId,
            long? guardianTravellerId,
            MedicalEquipmentChoice? medicalEquipment,
            UnaccompaniedMinorChoice? unaccompaniedMinor)
            : base(args, AncillaryProfile.AssistedTravel)
        {
            var isMedicalEquipment = VariantCode == AncillaryVariant.MedicalEquipment;
            var isUnaccompaniedMinor = VariantCode == AncillaryVariant.UnaccompaniedMinor;

            EnsureValid(assistanceKind == KindOf(VariantCode), nameof(AssistanceKind));
            EnsurePresent(VariantCode is not (AncillaryVariant.Wheelchair or AncillaryVariant.DisabilityAssistance) || !string.IsNullOrWhiteSpace(assistanceSsrCode), nameof(AssistanceSsrCode));
            EnsurePresent(VariantCode != AncillaryVariant.Bassinet || infantTravellerId is not null, nameof(InfantTravellerId));
            EnsurePresent(VariantCode != AncillaryVariant.Bassinet || guardianTravellerId is not null, nameof(GuardianTravellerId));
            EnsurePresent(!isMedicalEquipment || medicalEquipment is not null, nameof(MedicalEquipment));
            EnsureValid(isMedicalEquipment || medicalEquipment is null, nameof(MedicalEquipment));
            EnsureValid(medicalEquipment is null || IsComplete(medicalEquipment), nameof(MedicalEquipment));
            EnsurePresent(!isUnaccompaniedMinor || unaccompaniedMinor is not null, nameof(UnaccompaniedMinor));
            EnsureValid(isUnaccompaniedMinor || unaccompaniedMinor is null, nameof(UnaccompaniedMinor));
            EnsureValid(unaccompaniedMinor is null || IsComplete(unaccompaniedMinor), nameof(UnaccompaniedMinor));

            AssistanceKind = assistanceKind;
            AssistanceSsrCode = assistanceSsrCode;
            InfantTravellerId = infantTravellerId;
            GuardianTravellerId = guardianTravellerId;
            MedicalEquipment = medicalEquipment;
            UnaccompaniedMinor = unaccompaniedMinor;
        }

        public AssistanceKind AssistanceKind { get; private set; }

        public string? AssistanceSsrCode { get; private set; }

        public long? InfantTravellerId { get; private set; }

        public long? GuardianTravellerId { get; private set; }

        public MedicalEquipmentChoice? MedicalEquipment { get; private set; }

        public UnaccompaniedMinorChoice? UnaccompaniedMinor { get; private set; }

        private static bool IsComplete(MedicalEquipmentChoice medicalEquipment)
            => !string.IsNullOrWhiteSpace(medicalEquipment.EquipmentCode)
               && medicalEquipment.OxygenUnits is null or > 0
               && medicalEquipment.OxygenFlowRateLitersPerMinute is null or > 0;

        private static bool IsComplete(UnaccompaniedMinorChoice unaccompaniedMinor)
            => !string.IsNullOrWhiteSpace(unaccompaniedMinor.GuardianHandoffContact)
               && !string.IsNullOrWhiteSpace(unaccompaniedMinor.GuardianPickupContact);

        private static AssistanceKind KindOf(string variantCode)
            => variantCode switch
            {
                AncillaryVariant.Wheelchair => AssistanceKind.Wheelchair,
                AncillaryVariant.DisabilityAssistance => AssistanceKind.DisabilityAssistance,
                AncillaryVariant.MedicalEquipment => AssistanceKind.MedicalEquipment,
                AncillaryVariant.Bassinet => AssistanceKind.Bassinet,
                _ => AssistanceKind.UnaccompaniedMinor
            };
    }
}
