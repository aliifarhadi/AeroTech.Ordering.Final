using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderTraveller : Entity<long>
    {
        private readonly List<OrderTravellerDocument> _documents = new();
        private readonly List<TravellerProfileRevision> _profileRevisions = new();

        private OrderTraveller()
        {
        }

        public OrderTraveller(
            long id,
            long orderId,
            int index,
            string? sourceTravellerRef,
            PassengerTypeCode passengerType,
            AgeRange ageRange)
        {
            Id = id;
            OrderId = orderId;
            Index = index;
            SourceTravellerRef = sourceTravellerRef;
            PassengerType = passengerType;
            AgeRange = ageRange;
            Status = OrderTravellerStatus.Active;
        }

        public long OrderId { get; private set; }

        public int Index { get; private set; }

        public string? SourceTravellerRef { get; private set; }

        public PassengerTypeCode PassengerType { get; private set; }

        public AgeRange AgeRange { get; private set; }

        public long? InfantParentTravellerId { get; private set; }

        public long CurrentProfileRevisionId { get; private set; }

        public OrderTravellerStatus Status { get; private set; }

        public IReadOnlyCollection<OrderTravellerDocument> Documents => _documents.AsReadOnly();

        public IReadOnlyCollection<TravellerProfileRevision> ProfileRevisions => _profileRevisions.AsReadOnly();

        internal void AddDocument(OrderTravellerDocument document) => _documents.Add(document);

        internal void AddProfileRevision(TravellerProfileRevision revision)
        {
            _profileRevisions.Add(revision);
            CurrentProfileRevisionId = revision.Id;
        }

        internal void AssignInfantParent(long parentTravellerId) => InfantParentTravellerId = parentTravellerId;
    }
}
