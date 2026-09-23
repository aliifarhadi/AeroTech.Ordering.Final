using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderTravellerDocument : Entity<long>
    {
        private OrderTravellerDocument()
        {
        }

        public OrderTravellerDocument(
            long id,
            long orderTravellerId,
            TravellerDocumentType type,
            string number,
            DateOnly? expiryDate,
            int issuanceCountryId,
            string holder)
        {
            Id = id;
            OrderTravellerId = orderTravellerId;
            Type = type;
            Number = number;
            ExpiryDate = expiryDate;
            IssuanceCountryId = issuanceCountryId;
            Holder = holder;
        }

        public long OrderTravellerId { get; private set; }

        public TravellerDocumentType Type { get; private set; }

        public string Number { get; private set; } = default!;

        public DateOnly? ExpiryDate { get; private set; }

        public int IssuanceCountryId { get; private set; }

        public string Holder { get; private set; } = default!;
    }
}
