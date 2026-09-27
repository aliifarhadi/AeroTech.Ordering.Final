using AeroTech.Ordering.Query.DocumentStockAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Queries.GetDocumentStockById.Backoffice
{
    public sealed class BackofficeGetDocumentStockByIdQueryHandler : IRequestHandler<BackofficeGetDocumentStockByIdQuery, BackofficeDocumentStockDto?>
    {
        private readonly IGetDocumentStockByIdService _service;

        public BackofficeGetDocumentStockByIdQueryHandler(IGetDocumentStockByIdService service) => _service = service;

        public Task<BackofficeDocumentStockDto?> Handle(BackofficeGetDocumentStockByIdQuery query, CancellationToken cancellationToken)
            => _service.ExecuteAsync(query.DocumentStockId, cancellationToken);
    }
}
