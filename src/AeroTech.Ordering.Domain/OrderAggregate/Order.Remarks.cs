using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        private const int RemarkTextMaxLength = 512;

        public OrderRemark AddRemark(CreateOrderRemarkArgs args, IIdGenerator idGenerator, IClock clock)
            => AddRemarkInternal(args, idGenerator, clock.GetDateTime(), supersedesRemarkId: null);

        public OrderRemark ReviseRemark(long remarkId, CreateOrderRemarkArgs args, IIdGenerator idGenerator, IClock clock)
        {
            var existing = _remarks.FirstOrDefault(remark => remark.Id == remarkId)
                           ?? throw ExceptionFactory.RemarkNotFound();

            if (existing.Status != OrderRemarkStatus.Active)
                throw ExceptionFactory.OnlyActiveRemarkCanBeModified();

            var successor = AddRemarkInternal(args, idGenerator, clock.GetDateTime(), existing.Id);
            existing.Supersede();

            return successor;
        }

        private OrderRemark AddRemarkInternal(
            CreateOrderRemarkArgs args,
            IIdGenerator idGenerator,
            DateTimeOffset createdAt,
            long? supersedesRemarkId)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
                throw ExceptionFactory.RemarkTextIsRequired();

            if (args.Text.Length > RemarkTextMaxLength)
                throw ExceptionFactory.RemarkTextTooLong(RemarkTextMaxLength);

            if (args.Scope == OrderRemarkScope.Document)
                throw ExceptionFactory.DocumentScopedRemarkIsNotSupportedYet();

            var remark = new OrderRemark(
                idGenerator.NewId(),
                Id,
                args.Type,
                args.Visibility,
                args.Scope,
                travellerId: null,
                segmentId: null,
                orderItemId: null,
                orderServiceId: null,
                args.Text,
                args.CategoryCode,
                args.IsPrintedOnItinerary,
                args.IsPrintedOnInvoice,
                supersedesRemarkId,
                SalesContext.ActorId,
                createdAt);

            _remarks.Add(remark);
            return remark;
        }
    }
}
