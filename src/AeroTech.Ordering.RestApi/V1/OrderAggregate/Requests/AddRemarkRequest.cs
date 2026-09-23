using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark;

namespace AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests
{
    public sealed record AddRemarkRequest(long? SupersedesRemarkId, RemarkInput Remark);
}
