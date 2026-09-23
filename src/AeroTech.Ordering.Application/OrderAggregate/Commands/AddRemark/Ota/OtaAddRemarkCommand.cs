using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Ota
{
    public sealed record OtaAddRemarkCommand(
        long OrderId,
        long? SupersedesRemarkId,
        RemarkInput Remark) : IRequest<AddRemarkResult>, IAddRemarkCommand;
}
