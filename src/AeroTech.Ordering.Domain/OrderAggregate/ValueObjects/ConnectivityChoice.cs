using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class ConnectivityChoice : ValueObject
    {
        private ConnectivityChoice()
        {
        }

        public ConnectivityChoice(
            ConnectivityPlanKind planKind,
            int? durationMinutes,
            int? includedDataMb,
            int? maxDevices,
            PurchaseStage deliveryStage,
            int? deviceCount)
        {
            PlanKind = planKind;
            DurationMinutes = durationMinutes;
            IncludedDataMb = includedDataMb;
            MaxDevices = maxDevices;
            DeliveryStage = deliveryStage;
            DeviceCount = deviceCount;
        }

        public ConnectivityPlanKind PlanKind { get; private set; }

        public int? DurationMinutes { get; private set; }

        public int? IncludedDataMb { get; private set; }

        public int? MaxDevices { get; private set; }

        public PurchaseStage DeliveryStage { get; private set; }

        public int? DeviceCount { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return PlanKind;
            yield return DurationMinutes;
            yield return IncludedDataMb;
            yield return MaxDevices;
            yield return DeliveryStage;
            yield return DeviceCount;
        }
    }
}
