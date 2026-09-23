using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public sealed record RemarkInput(
        OrderRemarkType Type,
        OrderRemarkVisibility Visibility,
        OrderRemarkScope Scope,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice);

    public interface IAddRemarkCommand
    {
        long OrderId { get; }

        long? SupersedesRemarkId { get; }

        RemarkInput Remark { get; }
    }
}
