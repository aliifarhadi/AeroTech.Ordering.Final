using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using AeroTech.Ordering.Query.DocumentStockAggregate.Models;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Synchronizer.DocumentStockAggregate
{
    public sealed class DocumentStockQueryDbSynchronizer : IDocumentStockQueryDbSynchronizer
    {
        private readonly OrderQueryDbContext _dbContext;

        public DocumentStockQueryDbSynchronizer(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public async Task ProjectAsync(DocumentStockReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            var stock = await _dbContext.DocumentStocks.FirstOrDefaultAsync(row => row.Id == snapshot.DocumentStockId, cancellationToken);

            if (stock is null)
            {
                stock = new DocumentStockReadModel { Id = snapshot.DocumentStockId };
                _dbContext.DocumentStocks.Add(stock);
            }

            stock.OwnerAirlineId = snapshot.OwnerAirlineId;
            stock.OfficeId = snapshot.OfficeId;
            stock.DocumentKind = snapshot.DocumentKind;
            stock.Prefix = snapshot.Prefix;
            stock.SerialWidth = snapshot.SerialWidth;
            stock.CheckDigitProfile = snapshot.CheckDigitProfile;
            stock.RangeFrom = snapshot.RangeFrom;
            stock.RangeTo = snapshot.RangeTo;
            stock.NextNumber = snapshot.NextNumber;
            stock.Status = snapshot.Status;
        }
    }
}
