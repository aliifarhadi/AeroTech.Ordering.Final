using MediatR;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark.Backoffice
{
    public sealed record BackofficeAddRemarkCommand(
        long OrderId,
        long? SupersedesRemarkId,
        RemarkInput Remark) : IRequest<AddRemarkResult>, IAddRemarkCommand;
}
