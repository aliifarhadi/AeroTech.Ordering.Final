using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.OtaPanel
{
    public sealed record OtaPanelAddRemarkCommand(
        long OrderId,
        long? SupersedesRemarkId,
        RemarkInput Remark) : IRequest<AddRemarkResult>, IAddRemarkCommand;
}
