using AeroTech.Ordering.Query.DocumentStockAggregate.Dto;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Queries.GetDocumentStockById
{
    public interface IGetDocumentStockByIdService
    {
        Task<BackofficeDocumentStockDto?> ExecuteAsync(long documentStockId, CancellationToken cancellationToken = default);
    }
}
