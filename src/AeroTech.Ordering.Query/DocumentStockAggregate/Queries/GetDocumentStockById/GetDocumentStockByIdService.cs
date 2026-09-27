using AeroTech.Ordering.Query.DocumentStockAggregate.Dto;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Query._Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Queries.GetDocumentStockById
{
    public sealed class GetDocumentStockByIdService : IGetDocumentStockByIdService
    {
        private readonly OrderQueryDbContext _dbContext;

        public GetDocumentStockByIdService(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public async Task<BackofficeDocumentStockDto?> ExecuteAsync(long documentStockId, CancellationToken cancellationToken = default)
        {
            var stock = await _dbContext.DocumentStocks
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == documentStockId, cancellationToken);

            return stock is null
                ? null
                : new BackofficeDocumentStockDto(
                    stock.Id,
                    stock.OwnerAirlineId,
                    stock.OfficeId,
                    EnumValueDto.Of(stock.DocumentKind),
                    stock.Prefix,
                    stock.SerialWidth,
                    stock.CheckDigitProfile,
                    stock.RangeFrom,
                    stock.RangeTo,
                    stock.NextNumber,
                    EnumValueDto.Of(stock.Status));
        }
    }
}
