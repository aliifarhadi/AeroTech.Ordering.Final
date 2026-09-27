namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock
{
    public interface IDefineDocumentStockService
    {
        Task<DocumentStockResult> DefineAsync(IDefineDocumentStockCommand command, CancellationToken cancellationToken = default);
    }
}
