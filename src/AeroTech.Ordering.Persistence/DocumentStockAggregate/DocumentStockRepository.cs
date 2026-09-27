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
            => _dbContext.DocumentStocks.FirstOrDefaultAsync(stock => stock.Id == id, cancellationToken);

        public Task ReloadAsync(DocumentStock stock, CancellationToken cancellationToken = default)
            => _dbContext.Entry(stock).ReloadAsync(cancellationToken);

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
