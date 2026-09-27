using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.RestApi.V1.DocumentStockAggregate.Requests
{
    public sealed record DefineDocumentStockRequest(
        int OwnerAirlineId,
        long? OfficeId,
        AccountableDocumentKind DocumentKind,
        string Prefix,
        int SerialWidth,
        string CheckDigitProfile,
        long RangeFrom,
        long RangeTo);
}
