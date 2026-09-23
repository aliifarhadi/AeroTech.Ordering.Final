using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public sealed class GetBackofficeOrderByIdService : IGetBackofficeOrderByIdService
    {
        private readonly IGetOrderByIdService _orders;
        private readonly IReferenceCodeReader _codes;

        public GetBackofficeOrderByIdService(IGetOrderByIdService orders, IReferenceCodeReader codes)
        {
            _orders = orders;
            _codes = codes;
        }

        public async Task<BackofficeOrderDto?> ExecuteAsync(long orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orders.ExecuteAsync(orderId, OrderQueryScope.Unrestricted, cancellationToken);

            if (order is null)
                return null;

            var codes = await _codes.ReadAsync(KeysOf(order), cancellationToken);

            return BackofficeOrderMapper.ToBackofficeOrder(order, codes);
        }

        private static ReferenceCodeKeys KeysOf(OrderDetailDto order)
            => new(
                [],
                [],
                [],
                [],
                order.OfficeKind is { } kind && order.OfficeId is { } officeId ? [new OfficeKey(kind, officeId)] : []);
    }
}
