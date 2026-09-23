using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public interface IAddRemarkService
    {
        Task<AddRemarkResult> AddAsync(
            long orderId,
            CreateOrderRemarkArgs remark,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);

        Task<AddRemarkResult> ReviseAsync(
            long orderId,
            long remarkId,
            CreateOrderRemarkArgs remark,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default);
    }
}
