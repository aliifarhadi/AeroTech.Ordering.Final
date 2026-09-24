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
            long reservationUnitId,
            OrderFulfillmentTargetAction action)
        {
            Id = id;
            FulfillmentTaskId = fulfillmentTaskId;
            ReservationUnitId = reservationUnitId;
            Action = action;
        }

        public long FulfillmentTaskId { get; private set; }

        public long ReservationUnitId { get; private set; }

        public OrderFulfillmentTargetAction Action { get; private set; }
    }
}
