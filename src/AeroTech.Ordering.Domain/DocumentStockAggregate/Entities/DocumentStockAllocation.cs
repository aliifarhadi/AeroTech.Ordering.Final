using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.DocumentStockAggregate.Entities
{
    public sealed class DocumentStockAllocation : Entity<long>
    {
        private DocumentStockAllocation()
        {
        }

        internal DocumentStockAllocation(
            long id,
            long documentStockId,
            long issueFulfillmentTaskId,
            string documentRole,
            long serial,
            string documentNumber,
            DateTimeOffset allocatedAt)
        {
            Id = id;
            DocumentStockId = documentStockId;
            IssueFulfillmentTaskId = issueFulfillmentTaskId;
            DocumentRole = documentRole;
            Serial = serial;
            DocumentNumber = documentNumber;
            State = StockNumberState.Reserved;
            AllocatedAt = allocatedAt;
        }

        public long DocumentStockId { get; private set; }

        public long IssueFulfillmentTaskId { get; private set; }

        public string DocumentRole { get; private set; } = default!;

        public long Serial { get; private set; }

        public string DocumentNumber { get; private set; } = default!;

        public StockNumberState State { get; private set; }

        public DateTimeOffset AllocatedAt { get; private set; }

        public DateTimeOffset? SettledAt { get; private set; }

        internal void Settle(StockNumberState state, DateTimeOffset settledAt)
        {
            State = state;
            SettledAt = settledAt;
        }
    }
}
