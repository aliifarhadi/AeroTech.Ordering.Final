using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using MediatR;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated.Backoffice
{
    public sealed class BackofficeGetOrdersPaginatedQuery : PaginationQuery, IRequest<GridData<OrderPaginatedRowDto>>
    {
        public Guid? OrderReference { get; set; }
        public string? SourceOfferId { get; set; }
        public OrderStatus? Status { get; set; }
        public SalesChannel? Channel { get; set; }
        public long? CustomerId { get; set; }
        public string? TravellerName { get; set; }
        public string? FlightNumber { get; set; }
        public DateTimeOffset? CreatedFrom { get; set; }
        public DateTimeOffset? CreatedTo { get; set; }
        public DateTimeOffset? DepartureFrom { get; set; }
        public DateTimeOffset? DepartureTo { get; set; }
    }
}
