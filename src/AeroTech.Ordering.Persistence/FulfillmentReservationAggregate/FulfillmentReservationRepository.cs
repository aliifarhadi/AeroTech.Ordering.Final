using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Persistence.FulfillmentReservationAggregate
{
    public sealed class FulfillmentReservationRepository : IFulfillmentReservationRepository
    {
        private readonly OrderingDbContext _dbContext;

        public FulfillmentReservationRepository(OrderingDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(FulfillmentReservation reservation, CancellationToken cancellationToken = default)
            => await _dbContext.FulfillmentReservations.AddAsync(reservation, cancellationToken);

        public Task<FulfillmentReservation?> GetAsync(long id, CancellationToken cancellationToken = default)
            => AggregateQuery().FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

        public async Task<IReadOnlyList<FulfillmentReservation>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default)
            => await AggregateQuery().Where(reservation => reservation.OrderId == orderId).ToListAsync(cancellationToken);

        private IQueryable<FulfillmentReservation> AggregateQuery()
            => _dbContext.FulfillmentReservations.Include(reservation => reservation.Units);
    }
}
