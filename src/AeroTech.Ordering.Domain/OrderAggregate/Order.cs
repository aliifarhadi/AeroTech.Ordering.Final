using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order : AggregateRoot<long>
    {
        private readonly List<OrderItem> _items = new();
        private readonly List<OrderService> _services = new();
        private readonly List<OrderTraveller> _travellers = new();
        private readonly List<OrderContact> _contacts = new();
        private readonly List<OrderJourney> _journeys = new();
        private readonly List<OrderSegment> _segments = new();
        private readonly List<PricingLine> _pricingLines = new();
        private readonly List<OrderChange> _changes = new();
        private readonly List<OrderRemark> _remarks = new();

        private Order()
        {
        }

        private Order(
            long id,
            Guid orderReference,
            long customerId,
            SalesContext salesContext,
            int currencyId,
            string sourceOfferId,
            DateTimeOffset? lastTicketingDate,
            DateTimeOffset createdAt)
        {
            Id = id;
            OrderReference = orderReference;
            CustomerId = customerId;
            SalesContext = salesContext;
            CurrencyId = currencyId;
            SourceOfferId = sourceOfferId;
            LastTicketingDate = lastTicketingDate;
            Status = OrderStatus.Created;
            CommercialVersion = 1;
            CustomerTotal = 0m;
            CreatedAt = createdAt;
        }

        public Guid OrderReference { get; private set; }

        public long CustomerId { get; private set; }

        public SalesContext SalesContext { get; private set; } = default!;

        public int CurrencyId { get; private set; }

        public string SourceOfferId { get; private set; } = default!;

        public string? RecordLocator { get; private set; }

        public DateTimeOffset? LastTicketingDate { get; private set; }

        public OrderStatus Status { get; private set; }

        public int CommercialVersion { get; private set; }

        public decimal CustomerTotal { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        public IReadOnlyCollection<OrderService> Services => _services.AsReadOnly();

        public IReadOnlyCollection<OrderTraveller> Travellers => _travellers.AsReadOnly();

        public IReadOnlyCollection<OrderContact> Contacts => _contacts.AsReadOnly();

        public IReadOnlyCollection<OrderJourney> Journeys => _journeys.AsReadOnly();

        public IReadOnlyCollection<OrderSegment> Segments => _segments.AsReadOnly();

        public IReadOnlyCollection<PricingLine> PricingLines => _pricingLines.AsReadOnly();

        public IReadOnlyCollection<OrderChange> Changes => _changes.AsReadOnly();

        public IReadOnlyCollection<OrderRemark> Remarks => _remarks.AsReadOnly();

        private void TransitionTo(OrderStatus status) => Status = status;

        private void SetCustomerTotal(decimal customerTotal) => CustomerTotal = customerTotal;
    }
}
