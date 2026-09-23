using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public static class AddRemarkArgsMapper
    {
        public static CreateOrderRemarkArgs ToArgs(RemarkInput remark)
            => new(
                remark.Type,
                remark.Visibility,
                remark.Scope,
                remark.Text,
                remark.CategoryCode,
                remark.IsPrintedOnItinerary,
                remark.IsPrintedOnInvoice);
    }
}
