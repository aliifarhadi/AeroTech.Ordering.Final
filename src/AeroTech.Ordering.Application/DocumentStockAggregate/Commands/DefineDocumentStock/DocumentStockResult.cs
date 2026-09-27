using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock
{
    public sealed record DocumentStockResult(
        long DocumentStockId,
        int OwnerAirlineId,
        long? OfficeId,
        AccountableDocumentKind DocumentKind,
        string Prefix,
        int SerialWidth,
        string CheckDigitProfile,
        long RangeFrom,
        long RangeTo,
        long NextNumber,
        DocumentStockStatus Status);
}
