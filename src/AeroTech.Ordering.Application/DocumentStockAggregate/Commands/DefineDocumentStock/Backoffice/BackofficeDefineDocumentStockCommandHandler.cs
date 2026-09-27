using MediatR;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock.Backoffice
{
    public sealed class BackofficeDefineDocumentStockCommandHandler : IRequestHandler<BackofficeDefineDocumentStockCommand, DocumentStockResult>
    {
        private readonly IDefineDocumentStockService _service;

        public BackofficeDefineDocumentStockCommandHandler(IDefineDocumentStockService service) => _service = service;

        public Task<DocumentStockResult> Handle(BackofficeDefineDocumentStockCommand command, CancellationToken cancellationToken)
            => _service.DefineAsync(command, cancellationToken);
    }
}
