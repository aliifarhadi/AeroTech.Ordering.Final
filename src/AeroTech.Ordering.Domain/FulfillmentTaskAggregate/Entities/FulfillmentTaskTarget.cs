using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentTaskAggregate.Entities
{
    public sealed class FulfillmentTaskTarget : Entity<long>
    {
        private FulfillmentTaskTarget()
        {
        }

        internal FulfillmentTaskTarget(
            long id,
            long fulfillmentTaskId,
            FulfillmentTargetKind targetKind,
            long targetId,
            OrderFulfillmentTargetAction action)
        {
            Id = id;
            FulfillmentTaskId = fulfillmentTaskId;
            TargetKind = targetKind;
            TargetId = targetId;
            Action = action;
        }

        public long FulfillmentTaskId { get; private set; }

        public FulfillmentTargetKind TargetKind { get; private set; }

        public long TargetId { get; private set; }

        public OrderFulfillmentTargetAction Action { get; private set; }
    }
}
