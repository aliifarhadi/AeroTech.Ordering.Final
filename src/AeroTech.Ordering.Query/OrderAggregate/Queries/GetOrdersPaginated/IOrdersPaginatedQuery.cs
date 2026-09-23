using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated
{
    public interface IOrdersPaginatedQuery
    {
        Guid? OrderReference { get; }

        string? SourceOfferId { get; }

        OrderStatus? Status { get; }

        SalesChannel? Channel { get; }

        string? TravellerName { get; }

        string? FlightNumber { get; }

        DateTimeOffset? CreatedFrom { get; }

        DateTimeOffset? CreatedTo { get; }

        DateTimeOffset? DepartureFrom { get; }

        DateTimeOffset? DepartureTo { get; }

        int PageNumber { get; }

        int PageSize { get; }
    }
}
