using System.Globalization;
using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query.OrderAggregate.Models;
using AeroTech.Ordering.Query._Shared.Authorization;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Query._Shared.Enums;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;
using AeroTech.Ordering.ReferenceData.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrdersPaginated
{
    public sealed class GetOrdersPaginatedService : IGetOrdersPaginatedService
    {
        private readonly OrderQueryDbContext _dbContext;
        private readonly IReferenceCodeReader _codes;

        public GetOrdersPaginatedService(OrderQueryDbContext dbContext, IReferenceCodeReader codes)
        {
            _dbContext = dbContext;
            _codes = codes;
        }

        public async Task<GridData<OrderPaginatedRowDto>> ExecuteAsync(
            IOrdersPaginatedQuery query,
            OrderQueryScope scope,
            CancellationToken cancellationToken = default)
        {
            var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 10;

            var rows = from order in _dbContext.Orders.AsNoTracking()
                       join customer in _dbContext.Customers on order.CustomerId equals customer.Id into customerJoin
                       from customer in customerJoin.DefaultIfEmpty()
                       where (scope.CustomerId == null || order.CustomerId == scope.CustomerId)
                             && (query.OrderReference == null || order.OrderReference == query.OrderReference)
                             && (query.SourceOfferId == null || order.SourceOfferId == query.SourceOfferId)
                             && (query.Status == null || order.Status == query.Status)
                             && (query.Channel == null || order.Channel == query.Channel)
                             && (query.CreatedFrom == null || order.CreatedAt >= query.CreatedFrom)
                             && (query.CreatedTo == null || order.CreatedAt <= query.CreatedTo)
                             && (query.TravellerName == null
                                 || _dbContext.OrderTravellers.Any(traveller =>
                                     traveller.OrderId == order.Id
                                     && (traveller.GivenName.Contains(query.TravellerName)
                                         || (traveller.Surname != null && traveller.Surname.Contains(query.TravellerName)))))
                             && (query.FlightNumber == null
                                 || _dbContext.OrderSegments.Any(segment =>
                                     segment.OrderId == order.Id
                                     && segment.FlightNumber != null
                                     && segment.FlightNumber.Contains(query.FlightNumber)))
                             && (query.DepartureFrom == null
                                 || _dbContext.OrderSegments.Any(segment =>
                                     segment.OrderId == order.Id && segment.SoldDeparture >= query.DepartureFrom))
                             && (query.DepartureTo == null
                                 || _dbContext.OrderSegments.Any(segment =>
                                     segment.OrderId == order.Id && segment.SoldDeparture <= query.DepartureTo))
                       select new OrderRow { Order = order, Customer = customer };

            var totalCount = await rows.LongCountAsync(cancellationToken);

            var page = await rows
                .OrderByDescending(row => row.Order.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var orderIds = page.Select(row => row.Order.Id).ToList();

            var travellersByOrder = (await _dbContext.OrderTravellers.AsNoTracking()
                    .Where(traveller => orderIds.Contains(traveller.OrderId))
                    .ToListAsync(cancellationToken))
                .ToLookup(traveller => traveller.OrderId);

            var segmentsByOrder = (await _dbContext.OrderSegments.AsNoTracking()
                    .Where(segment => orderIds.Contains(segment.OrderId))
                    .ToListAsync(cancellationToken))
                .ToLookup(segment => segment.OrderId);

            var segments = segmentsByOrder.SelectMany(group => group).ToList();

            var codes = await _codes.ReadAsync(
                new ReferenceCodeKeys(
                    segments.Select(segment => segment.MarketingAirlineId).Distinct().ToList(),
                    segments.SelectMany(segment => new[] { segment.OriginAirportId, segment.DestinationAirportId }).Distinct().ToList(),
                    page.Select(row => row.Order.CurrencyId).Distinct().ToList(),
                    [],
                    page.Select(row => row.Order)
                        .Where(order => order.OfficeKind.HasValue && order.OfficeId.HasValue)
                        .Select(order => new OfficeKey(order.OfficeKind!.Value, order.OfficeId!.Value))
                        .Distinct()
                        .ToList()),
                cancellationToken);

            var projected = page.Select(row => Map(
                row,
                travellersByOrder[row.Order.Id].OrderBy(traveller => traveller.Index).ToList(),
                segmentsByOrder[row.Order.Id].OrderBy(segment => segment.SoldDeparture).ToList(),
                codes));

            return GridData<OrderPaginatedRowDto>.Create(
                PaginatedList<OrderPaginatedRowDto>.Create(projected, pageNumber, pageSize, totalCount));
        }

        private static OrderPaginatedRowDto Map(
            OrderRow row,
            IReadOnlyList<OrderTravellerReadModel> travellers,
            IReadOnlyList<OrderSegmentReadModel> segments,
            ReferenceCodes codes)
        {
            var order = row.Order;

            return new OrderPaginatedRowDto
            {
                Id = order.Id.ToString(CultureInfo.InvariantCulture),
                OrderReference = order.OrderReference.ToString(),
                CreatedAt = order.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
                Channel = EnumValueDto.Of(order.Channel),
                SellingOffice = ToSellingOffice(order, codes),
                Customer = row.Customer?.SubjectName,
                CustomerNumber = row.Customer?.CustomerNumber,
                CustomerId = order.CustomerId,
                Passengers = travellers.Count,
                PassengerSummary = BuildPassengerSummary(travellers),
                Status = EnumValueDto.Of(order.Status),
                CustomerTotal = order.CustomerTotal.ToString("n2", CultureInfo.InvariantCulture),
                Currency = codes.Currency(order.CurrencyId),
                CommercialVersion = order.CommercialVersion,
                LastTicketingDate = order.LastTicketingDate,
                SourceOfferId = order.SourceOfferId,
                FlightSummary = BuildFlightSummary(segments, codes)
            };
        }

        private static SellingOfficeDto? ToSellingOffice(OrderReadModel order, ReferenceCodes codes)
        {
            if (order.OfficeKind is not { } kind || order.OfficeId is not { } officeId)
                return null;

            var office = codes.Office(kind, officeId);

            return new SellingOfficeDto(EnumValueDto.Of(kind), officeId, office?.Code, office?.Name);
        }

        private static PassengerSummaryDto BuildPassengerSummary(IReadOnlyList<OrderTravellerReadModel> travellers)
            => new()
            {
                Total = travellers.Count,
                Adults = travellers.Count(traveller => traveller.AgeRange == AgeRange.Adult),
                Children = travellers.Count(traveller => traveller.AgeRange == AgeRange.Child),
                Infants = travellers.Count(traveller => traveller.AgeRange == AgeRange.Infant),
                Initials = travellers.Select(Initial).ToList()
            };

        private static string Initial(OrderTravellerReadModel traveller)
        {
            var given = traveller.GivenName.Length == 0 ? string.Empty : traveller.GivenName[..1];
            var surname = string.IsNullOrEmpty(traveller.Surname) ? string.Empty : traveller.Surname[..1];
            return (given + surname).ToUpperInvariant();
        }

        private static FlightSummaryDto BuildFlightSummary(IReadOnlyList<OrderSegmentReadModel> segments, ReferenceCodes codes)
        {
            if (segments.Count == 0)
                return new FlightSummaryDto();

            var first = segments[0];
            var carrier = codes.Airline(first.MarketingAirlineId);
            var chain = new List<string>(segments.Count + 1);

            AppendAirport(chain, codes.Airport(first.OriginAirportId));

            foreach (var segment in segments)
                AppendAirport(chain, codes.Airport(segment.DestinationAirportId));

            return new FlightSummaryDto
            {
                FlightNumber = string.IsNullOrWhiteSpace(carrier) ? first.FlightNumber : $"{carrier} {first.FlightNumber}",
                DepartureDateTime = first.SoldDeparture,
                Stops = Math.Max(chain.Count - 2, 0),
                AirportIataCode = chain
            };
        }

        private static void AppendAirport(List<string> chain, string? iataCode)
        {
            if (string.IsNullOrWhiteSpace(iataCode))
                return;

            if (chain.Count > 0 && string.Equals(chain[^1], iataCode, StringComparison.OrdinalIgnoreCase))
                return;

            chain.Add(iataCode);
        }

        private sealed class OrderRow
        {
            public OrderReadModel Order { get; init; } = default!;
            public CustomerReadModel? Customer { get; init; }
        }
    }
}
