using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Persistence.ElectronicTicketAggregate
{
    public sealed class ElectronicTicketRepository : IElectronicTicketRepository
    {
        private readonly OrderingDbContext _dbContext;

        public ElectronicTicketRepository(OrderingDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(ElectronicTicket ticket, CancellationToken cancellationToken = default)
            => await _dbContext.ElectronicTickets.AddAsync(ticket, cancellationToken);

        public async Task<IReadOnlyList<ElectronicTicket>> ListByOrderAsync(long orderId, CancellationToken cancellationToken = default)
            => await _dbContext.ElectronicTickets
                .Include(ticket => ticket.Coupons)
                .Include(ticket => ticket.PriceLinks)
                .AsSplitQuery()
                .Where(ticket => ticket.CurrentServicingOrderId == orderId)
                .OrderBy(ticket => ticket.IssuedAt)
                .ThenBy(ticket => ticket.Id)
                .ToListAsync(cancellationToken);

        public Task<bool> AnyWithDocumentNumberAsync(IReadOnlyCollection<string> documentNumbers, CancellationToken cancellationToken = default)
            => _dbContext.ElectronicTickets.AnyAsync(ticket => documentNumbers.Contains(ticket.DocumentNumber), cancellationToken);
    }
}
