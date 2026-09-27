using AeroTech.Ordering.Domain.DocumentStockAggregate;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Projection
{
    internal static class DocumentStockReadModelSnapshotFactory
    {
        public static DocumentStockReadModelSnapshot ToReadModelSnapshot(this DocumentStock stock)
            => new(
                stock.Id,
                stock.OwnerAirlineId,
                stock.OfficeId,
                stock.DocumentKind,
                stock.Prefix,
                stock.SerialWidth,
                stock.CheckDigitProfile,
                stock.RangeFrom,
                stock.RangeTo,
                stock.NextNumber,
                stock.Status);
    }
}
