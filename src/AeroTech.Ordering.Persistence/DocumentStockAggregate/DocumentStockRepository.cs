using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Persistence.DocumentStockAggregate
{
    public sealed class DocumentStockRepository : IDocumentStockRepository
    {
        private readonly OrderingDbContext _dbContext;

        public DocumentStockRepository(OrderingDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(DocumentStock stock, CancellationToken cancellationToken = default)
            => await _dbContext.DocumentStocks.AddAsync(stock, cancellationToken);

        public Task<DocumentStock?> GetAsync(long id, CancellationToken cancellationToken = default)
            => _dbContext.DocumentStocks
                .Include(stock => stock.Allocations)
                .FirstOrDefaultAsync(stock => stock.Id == id, cancellationToken);

        public async Task ReloadAsync(DocumentStock stock, CancellationToken cancellationToken = default)
        {
            var entry = _dbContext.Entry(stock);

            await entry.ReloadAsync(cancellationToken);

            foreach (var allocation in stock.Allocations)
                await _dbContext.Entry(allocation).ReloadAsync(cancellationToken);

            await entry.Collection(item => item.Allocations).Query().LoadAsync(cancellationToken);
        }

        public Task<long?> FindOverlappingIdAsync(
            int ownerAirlineId,
            AccountableDocumentKind documentKind,
            string prefix,
            long rangeFrom,
            long rangeTo,
            CancellationToken cancellationToken = default)
            => _dbContext.DocumentStocks
                .Where(stock => stock.OwnerAirlineId == ownerAirlineId
                                && stock.DocumentKind == documentKind
                                && stock.Prefix == prefix
                                && stock.RangeFrom <= rangeTo
                                && rangeFrom <= stock.RangeTo)
                .Select(stock => (long?)stock.Id)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
