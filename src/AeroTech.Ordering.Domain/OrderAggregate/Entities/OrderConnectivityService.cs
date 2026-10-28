using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderConnectivityService : OrderAncillaryService
    {
        private OrderConnectivityService()
        {
        }

        public OrderConnectivityService(
            OrderAncillaryServiceArgs args,
            ConnectivityPlanKind planKind,
            int? durationMinutes,
            int? includedDataMb,
            int? maxDevices,
            PurchaseStage deliveryStage,
            int? deviceCount)
            : base(args, AncillaryProfile.Connectivity)
        {
            EnsureValid(deviceCount is null or > 0, nameof(DeviceCount));

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
    }
}
