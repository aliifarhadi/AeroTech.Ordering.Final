using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.OrderAggregate.Projection;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark
{
    public sealed class AddRemarkService : IAddRemarkService
    {
        private readonly IOrderRepository _orders;
        private readonly IOrderQueryDbSynchronizer _synchronizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIdGenerator _idGenerator;
        private readonly IClock _clock;

        public AddRemarkService(
            IOrderRepository orders,
            IOrderQueryDbSynchronizer synchronizer,
            IUnitOfWork unitOfWork,
            IIdGenerator idGenerator,
            IClock clock)
        {
            _orders = orders;
            _synchronizer = synchronizer;
            _unitOfWork = unitOfWork;
            _idGenerator = idGenerator;
            _clock = clock;
        }

        public Task<AddRemarkResult> AddAsync(
            long orderId,
            CreateOrderRemarkArgs remark,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => MutateAsync(orderId, authorization, order => order.AddRemark(remark, _idGenerator, _clock), cancellationToken);

        public Task<AddRemarkResult> ReviseAsync(
            long orderId,
            long remarkId,
            CreateOrderRemarkArgs remark,
            IOrderAuthorization authorization,
            CancellationToken cancellationToken = default)
            => MutateAsync(orderId, authorization, order => order.ReviseRemark(remarkId, remark, _idGenerator, _clock), cancellationToken);

        private async Task<AddRemarkResult> MutateAsync(
            long orderId,
            IOrderAuthorization authorization,
            Func<Order, OrderRemark> mutate,
            CancellationToken cancellationToken)
        {
            var order = await _orders.GetAsync(orderId, cancellationToken)
                        ?? throw ExceptionFactory.OrderNotFound(orderId);

            authorization.Ensure(order);

            var remark = mutate(order);

            await _synchronizer.ProjectRemarkedAsync(order.ToReadModelSnapshot(remark.CreatedAt), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AddRemarkResult(
                order.Id,
                remark.Id,
                remark.Status,
                remark.SupersedesRemarkId,
                order.CommercialVersion);
        }
    }
}
