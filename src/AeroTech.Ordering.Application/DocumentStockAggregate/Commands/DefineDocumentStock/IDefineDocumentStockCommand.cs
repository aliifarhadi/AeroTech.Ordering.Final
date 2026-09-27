using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.DocumentStockAggregate.Commands.DefineDocumentStock
{
    public interface IDefineDocumentStockCommand
    {
        int OwnerAirlineId { get; }

        long? OfficeId { get; }

        AccountableDocumentKind DocumentKind { get; }

        string Prefix { get; }

        int SerialWidth { get; }

        string CheckDigitProfile { get; }

        long RangeFrom { get; }

        long RangeTo { get; }
    }
}
