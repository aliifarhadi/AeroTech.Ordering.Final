using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderTravellerDocumentReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public long TravellerId { get; set; }

        public TravellerDocumentType Type { get; set; }

        public string Number { get; set; } = default!;

        public DateOnly? ExpiryDate { get; set; }

        public int IssuanceCountryId { get; set; }

        public string Holder { get; set; } = default!;
    }
}
