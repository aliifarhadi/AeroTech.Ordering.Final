using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderFarePricingUnit : Entity<long>
    {
        private readonly List<long> _coveredJourneyIds = new();
        private readonly List<OrderFareComponent> _fareComponents = new();

        private OrderFarePricingUnit()
        {
        }

        internal OrderFarePricingUnit(
            long id,
            long orderId,
            long createdByChangeId,
            int sequence,
            string sourceKind,
            FarePricingUnitType semanticType,
            IReadOnlyCollection<long> coveredJourneyIds)
        {
            if (coveredJourneyIds.Count == 0)
                throw ExceptionFactory.FarePricingUnitCoversNoJourney(sequence);

            Id = id;
            OrderId = orderId;
            CreatedByChangeId = createdByChangeId;
            Sequence = sequence;
            SourceKind = sourceKind;
            SemanticType = semanticType;
            _coveredJourneyIds.AddRange(coveredJourneyIds);
        }

        public long OrderId { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public int Sequence { get; private set; }

        public string SourceKind { get; private set; } = default!;

        public FarePricingUnitType SemanticType { get; private set; }

        public IReadOnlyCollection<long> CoveredJourneyIds => _coveredJourneyIds.AsReadOnly();

        public IReadOnlyCollection<OrderFareComponent> FareComponents => _fareComponents.AsReadOnly();

        internal void AddFareComponent(OrderFareComponent fareComponent) => _fareComponents.Add(fareComponent);
    }
}
