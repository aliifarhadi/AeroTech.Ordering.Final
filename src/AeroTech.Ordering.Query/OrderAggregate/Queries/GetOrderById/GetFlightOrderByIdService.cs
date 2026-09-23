using AeroTech.Ordering.Query.OrderAggregate.Dto;
using AeroTech.Ordering.Query._Shared.Authorization;
using AeroTech.Ordering.Query._Shared.ReferenceCodes;

namespace AeroTech.Ordering.Query.OrderAggregate.Queries.GetOrderById
{
    public sealed class GetFlightOrderByIdService : IGetFlightOrderByIdService
    {
        private readonly IGetOrderByIdService _orders;
        private readonly IReferenceCodeReader _codes;

        public GetFlightOrderByIdService(IGetOrderByIdService orders, IReferenceCodeReader codes)
        {
            _orders = orders;
            _codes = codes;
        }

        public async Task<FlightOrderDto?> ExecuteAsync(
            long orderId,
            OrderQueryScope scope,
            CancellationToken cancellationToken = default)
        {
            var order = await _orders.ExecuteAsync(orderId, scope, cancellationToken);

            if (order is null)
                return null;

            var codes = await _codes.ReadAsync(KeysOf(order), cancellationToken);

            return FlightOrderMapper.ToFlightOrder(order, codes);
        }

        private static ReferenceCodeKeys KeysOf(OrderDetailDto order)
        {
            var segments = order.Journeys.SelectMany(journey => journey.Segments).ToList();

            return new ReferenceCodeKeys(
                segments.SelectMany(segment => new[] { segment.MarketingAirlineId, segment.OperatingAirlineId }).Distinct().ToList(),
                segments.SelectMany(segment => new[] { segment.OriginAirportId, segment.DestinationAirportId }).Distinct().ToList(),
                [order.CurrencyId],
                order.Travellers
                    .SelectMany(traveller => traveller.Documents
                        .Select(document => (int?)document.IssuanceCountryId)
                        .Append(traveller.NationalityId)
                        .Append(traveller.CountryOfResidenceId))
                    .OfType<int>()
                    .Distinct()
                    .ToList(),
                []);
        }
    }
}
