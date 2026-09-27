using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.DocumentStockAggregate.Projection;
using AeroTech.Ordering.Application.DocumentStockAggregate.Services;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock
{
    public sealed class DefineDocumentStockService : IDefineDocumentStockService
    {
        private readonly IDocumentStockRepository _stocks;
        private readonly IDocumentStockLock _stockLock;
        private readonly IDocumentStockQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;

        public DefineDocumentStockService(
            IDocumentStockRepository stocks,
            IDocumentStockLock stockLock,
            IDocumentStockQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator)
        {
            _stocks = stocks;
            _stockLock = stockLock;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
        }

        public async Task<DocumentStockResult> DefineAsync(IDefineDocumentStockCommand command, CancellationToken cancellationToken = default)
        {
            var stock = DocumentStock.Define(
                _idGenerator.NewId(),
                command.OwnerAirlineId,
                command.OfficeId,
                command.DocumentKind,
                command.Prefix,
                command.SerialWidth,
                command.CheckDigitProfile,
                command.RangeFrom,
                command.RangeTo);

            await using var rangeLock = await _stockLock.AcquireRangeAsync(stock.OwnerAirlineId, stock.DocumentKind, stock.Prefix, cancellationToken);

            if (await _stocks.FindOverlappingIdAsync(stock.OwnerAirlineId, stock.DocumentKind, stock.Prefix, stock.RangeFrom, stock.RangeTo, cancellationToken) is { } overlappingId)
                throw ExceptionFactory.DocumentStockRangeOverlaps(overlappingId);

            await _stocks.AddAsync(stock, cancellationToken);
            await _synchronizer.ProjectAsync(stock.ToReadModelSnapshot(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DocumentStockResult(
                stock.Id,
                stock.OwnerAirlineId,
                stock.OfficeId,
                stock.DocumentKind,
                stock.Prefix,
                stock.SerialWidth,
                stock.CheckDigitProfile,
                stock.RangeFrom,
                stock.RangeTo,
                stock.NextNumber,
                stock.Status);
        }
    }
}
