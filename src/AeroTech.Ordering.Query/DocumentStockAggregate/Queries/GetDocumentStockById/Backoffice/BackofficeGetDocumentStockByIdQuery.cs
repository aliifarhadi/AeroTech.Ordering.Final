using AeroTech.Ordering.Query.DocumentStockAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Queries.GetDocumentStockById.Backoffice
{
    public sealed record BackofficeGetDocumentStockByIdQuery(long DocumentStockId) : IRequest<BackofficeDocumentStockDto?>;
}
