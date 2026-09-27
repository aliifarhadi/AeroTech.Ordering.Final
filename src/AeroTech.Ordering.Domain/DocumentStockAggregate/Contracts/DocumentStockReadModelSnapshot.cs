using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts
{
    public sealed record DocumentStockReadModelSnapshot(
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
