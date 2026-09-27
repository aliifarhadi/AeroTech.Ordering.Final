using AeroTech.Messages.Ordering.Enums;
using MediatR;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock.Backoffice
{
    public sealed record BackofficeDefineDocumentStockCommand(
        int OwnerAirlineId,
        long? OfficeId,
        AccountableDocumentKind DocumentKind,
        string Prefix,
        int SerialWidth,
        string CheckDigitProfile,
        long RangeFrom,
        long RangeTo) : IRequest<DocumentStockResult>, IDefineDocumentStockCommand;
}
