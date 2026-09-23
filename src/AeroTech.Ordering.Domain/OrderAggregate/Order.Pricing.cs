using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        private void BuildPricing(OfferReader reader, IIdGenerator idGenerator, long changeId, DateTimeOffset createdAt)
        {
            foreach (var traveller in _travellers.OrderBy(traveller => traveller.Index))
            {
                foreach (var bound in reader.BoundsInSequence())
                {
                    foreach (var coupon in reader.BoundCoupons(traveller.SourceTravellerRef!, bound.BoundId))
                    {
                        var service = AirServiceFor(traveller.Id, coupon.FlightId);

                        foreach (var line in coupon.PriceLines)
                            AddCouponLine(reader, idGenerator, changeId, createdAt, line, traveller, service);
                    }
                }
            }

            foreach (var line in reader.OrderChargeLines())
                AddOrderScopedLine(reader, idGenerator, changeId, createdAt, line);
        }

        private void AddCouponLine(
            OfferReader reader,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt,
            OfferPriceLine source,
            OrderTraveller traveller,
            OrderAirTransportService service)
        {
            var line = NewPricingLine(
                reader,
                idGenerator,
                changeId,
                createdAt,
                source,
                PricingLineScope.OrderService,
                PricingLineTreatment.CustomerPrice);

            line.AddAllocation(new PricingAllocation(
                idGenerator.NewId(),
                line.Id,
                service.OrderItemId,
                service.Id,
                SegmentJourneyId(service.SegmentId),
                service.SegmentId,
                traveller.Id,
                line.Amount,
                line.CurrencyId,
                line.EquivalentAmount,
                line.EquivalentCurrencyId));

            _pricingLines.Add(line);
        }

        private void AddOrderScopedLine(
            OfferReader reader,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt,
            OfferPriceLine source)
        {
            var treatment = CategoryOf(source) == OrderPricingLineCategory.Commission
                ? PricingLineTreatment.SettlementOnly
                : PricingLineTreatment.CustomerPrice;

            _pricingLines.Add(NewPricingLine(
                reader,
                idGenerator,
                changeId,
                createdAt,
                source,
                PricingLineScope.Order,
                treatment));
        }

        private PricingLine NewPricingLine(
            OfferReader reader,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt,
            OfferPriceLine source,
            PricingLineScope scope,
            PricingLineTreatment treatment)
        {
            var category = CategoryOf(source);

            return new PricingLine(
                idGenerator.NewId(),
                Id,
                changeId,
                OrderPricingReason.InitialSale,
                scope,
                category,
                SubCategoryOf(category),
                source.Amount < 0 ? OrderPricingLineDirection.Debit : OrderPricingLineDirection.Credit,
                treatment,
                source.Code,
                source.Name,
                source.Reference,
                Math.Abs(source.Amount),
                reader.SourceCurrencyId(source),
                Math.Abs(source.EquivalentAmount),
                reader.EquivalentCurrencyId(source),
                ToSnapshot(reader.Rate(source.RateOfExchangePeriodId)),
                refundability: null,
                createdAt);
        }

        private void ReconcileTotals()
        {
            foreach (var line in _pricingLines)
            {
                if (line.Allocations.Count == 0)
                    continue;

                if (line.Allocations.Sum(allocation => allocation.Amount) != line.Amount)
                    throw ExceptionFactory.PricingAllocationsDoNotReconcile(line.Id);

                if (line.Allocations.Sum(allocation => allocation.EquivalentAmount) != line.EquivalentAmount)
                    throw ExceptionFactory.PricingAllocationsDoNotReconcile(line.Id);
            }

            var customerLines = _pricingLines.Where(line => line.Treatment == PricingLineTreatment.CustomerPrice).ToList();

            if (customerLines.Any(line => line.EquivalentCurrencyId != CurrencyId))
                throw ExceptionFactory.CustomerPriceLineCurrencyDoesNotMatchOrder();

            SetCustomerTotal(customerLines.Sum(line => line.SignedEquivalentAmount));

            foreach (var item in _items)
                item.SetAcceptedTotal(customerLines
                    .SelectMany(line => line.Allocations.Select(allocation => (line, allocation)))
                    .Where(pair => pair.allocation.OrderItemId == item.Id)
                    .Sum(pair => pair.line.Direction == OrderPricingLineDirection.Credit
                        ? pair.allocation.EquivalentAmount
                        : -pair.allocation.EquivalentAmount));
        }

        private static OrderPricingLineCategory CategoryOf(OfferPriceLine source)
            => source.Category switch
            {
                OfferPriceCategory.Fare => OrderPricingLineCategory.Fare,
                OfferPriceCategory.Tax => OrderPricingLineCategory.Tax,
                OfferPriceCategory.Fee => OrderPricingLineCategory.Fee,
                OfferPriceCategory.Surcharge => OrderPricingLineCategory.CarrierImposedSurcharge,
                _ => throw ExceptionFactory.OfferPriceCategoryIsNotRecognised(source.Category)
            };

        private static OrderPricingLineSubCategory SubCategoryOf(OrderPricingLineCategory category)
            => category switch
            {
                OrderPricingLineCategory.Fare => OrderPricingLineSubCategory.BaseFare,
                OrderPricingLineCategory.Tax => OrderPricingLineSubCategory.Tax,
                _ => OrderPricingLineSubCategory.ServiceFee
            };

        private static ExchangeRateSnapshot? ToSnapshot(OfferRate? rate)
            => rate is null
                ? null
                : new ExchangeRateSnapshot(
                    rate.FromCurrencyId,
                    rate.ToCurrencyId,
                    rate.Rate,
                    rate.DecimalPlaces,
                    rate.RateOfExchangePeriodId);

        private OrderAirTransportService AirServiceFor(long travellerId, long flightId)
        {
            var segment = SegmentOf(flightId);

            return _services
                       .OfType<OrderAirTransportService>()
                       .FirstOrDefault(service => service.TravellerId == travellerId && service.SegmentId == segment.Id)
                   ?? throw ExceptionFactory.OrderHasNoAirServiceForFlight(flightId);
        }

        private long SegmentJourneyId(long segmentId)
            => _segments.First(segment => segment.Id == segmentId).OrderJourneyId;
    }
}
