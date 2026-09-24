using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly OrderingDbContext _dbContext;

        public OrderRepository(OrderingDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
            => await _dbContext.Orders.AddAsync(order, cancellationToken);

        public Task<Order?> GetAsync(long id, CancellationToken cancellationToken = default)
            => AggregateQuery().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

        public Task<Order?> GetByReferenceAsync(Guid orderReference, CancellationToken cancellationToken = default)
            => AggregateQuery().FirstOrDefaultAsync(order => order.OrderReference == orderReference, cancellationToken);

        public Task<bool> RecordLocatorExistsAsync(string recordLocator, CancellationToken cancellationToken = default)
            => _dbContext.Orders.AnyAsync(order => order.RecordLocator == recordLocator, cancellationToken);

        public async Task<IReadOnlyList<long>> ListReservableIdsPastLastTicketingDateAsync(
            DateTimeOffset now,
            int batchSize,
            CancellationToken cancellationToken = default)
            => await _dbContext.Orders
                .Where(order => order.LastTicketingDate <= now && Order.ReservableStatuses.Contains(order.Status))
                .OrderBy(order => order.LastTicketingDate)
                .Select(order => order.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

        private IQueryable<Order> AggregateQuery()
            => _dbContext.Orders
                .Include(order => order.Items)
                .Include(order => order.Services)
                .Include(order => order.Travellers).ThenInclude(traveller => traveller.Documents)
                .Include(order => order.Travellers).ThenInclude(traveller => traveller.ProfileRevisions)
                .Include(order => order.Contacts).ThenInclude(contact => contact.ContactPoints)
                .Include(order => order.Journeys)
                .Include(order => order.Segments).ThenInclude(segment => segment.Legs)
                .Include(order => order.PricingLines).ThenInclude(line => line.Allocations)
                .Include(order => order.FarePricingUnits).ThenInclude(pricingUnit => pricingUnit.FareComponents)
                .Include(order => order.Changes)
                .Include(order => order.Remarks)
                .AsSplitQuery();
    }
}
