using AeroTech.Ordering.Query._Shared.Enums;

namespace AeroTech.Ordering.Query.DocumentStockAggregate.Dto
{
    public sealed record BackofficeDocumentStockDto(
        long Id,
        int OwnerAirlineId,
        long? OfficeId,
        EnumValueDto DocumentKind,
        string Prefix,
        int SerialWidth,
        string CheckDigitProfile,
        long RangeFrom,
        long RangeTo,
        long NextNumber,
        EnumValueDto Status);
}
