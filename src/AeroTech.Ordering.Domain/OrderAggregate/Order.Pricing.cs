using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain.Providers.Offer;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        public IReadOnlyList<FarePricingAtom> FarePricingAtomsCovering(IReadOnlyCollection<long> airServiceIds)
            => _farePricingUnits
                .OrderBy(pricingUnit => pricingUnit.Sequence)
                .SelectMany(pricingUnit => pricingUnit.PricingAtoms())
                .Where(atom => atom.AirServiceIds.Overlaps(airServiceIds))
                .ToList();

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
                taxJurisdiction: null,
                sourceTaxTreatment: null,
                sourceFeeApplicationUnit: null,
                createdAt);
        }

        private IReadOnlyList<AcceptedPriceComponent> AcceptedPriceComponents(string reference, ResolvedAncillaryPricing pricing)
        {
            var components = pricing.TaxLines.Concat(pricing.FeeLines).ToList();

            if (pricing.PricingId <= 0 || pricing.PricingRateId <= 0 || pricing.PricingVersion <= 0)
                throw ExceptionFactory.AncillaryPricingReferenceIsInvalid(reference);

            if (!pricing.IsUnitTotalComplete)
                throw ExceptionFactory.AncillaryPriceIsIncomplete(reference);

            if (pricing.CurrencyId != CurrencyId)
                throw ExceptionFactory.AncillaryPriceCurrencyDoesNotMatchOrder(reference, pricing.CurrencyId, CurrencyId);

            if (components.FirstOrDefault(component => component.CurrencyId != pricing.CurrencyId) is { } foreign)
                throw ExceptionFactory.AncillaryPriceCurrencyDoesNotMatchOrder(reference, foreign.CurrencyId, CurrencyId);

            if (pricing.UnitAmount < 0 || pricing.TotalAmount < 0 || pricing.BaseAmount < 0 || components.Any(component => component.Amount < 0))
                throw ExceptionFactory.AncillaryPriceAmountIsNegative(reference);

            if (pricing.TaxLines.FirstOrDefault(tax => tax.TaxTreatment is not (TaxTreatment.IncludedInBase or TaxTreatment.AddedToBase)) is { } tax)
                throw ExceptionFactory.AncillaryTaxTreatmentIsNotAccepted(reference, tax.Code, tax.TaxTreatment);

            if (pricing.FeeLines.FirstOrDefault(fee => fee.FeeApplicationUnit != FeeApplicationUnit.Item) is { } fee)
                throw ExceptionFactory.AncillaryFeeIsNotAppliedPerItem(reference, fee.Code, fee.FeeApplicationUnit);

            var netBase = pricing.BaseAmount - pricing.TaxLines.Where(tax => tax.TaxTreatment == TaxTreatment.IncludedInBase).Sum(tax => tax.Amount);
            var unitTotal = netBase + components.Sum(component => component.Amount);

            if (netBase < 0 || unitTotal != pricing.UnitAmount)
                throw ExceptionFactory.AncillaryPriceDoesNotReconcile(reference, unitTotal, pricing.UnitAmount);

            if (pricing.UnitAmount * pricing.Quantity != pricing.TotalAmount)
                throw ExceptionFactory.AncillaryPriceDoesNotReconcile(reference, pricing.UnitAmount * pricing.Quantity, pricing.TotalAmount);

            var accepted = new List<AcceptedPriceComponent>
            {
                new(OrderPricingLineCategory.Ancillary, OrderPricingLineSubCategory.AncillaryBase, null, netBase * pricing.Quantity, null, null, null)
            };

            accepted.AddRange(pricing.TaxLines.Select(tax => AcceptedComponentOf(tax, OrderPricingLineCategory.Tax, OrderPricingLineSubCategory.Tax, pricing.Quantity)));
            accepted.AddRange(pricing.FeeLines.Select(fee => AcceptedComponentOf(fee, OrderPricingLineCategory.Fee, OrderPricingLineSubCategory.ServiceFee, pricing.Quantity)));

            return accepted.Where(component => component.Amount != 0).ToList();
        }

        private IReadOnlyList<PricingLine> PricingLinesFor(
            IReadOnlyList<AcceptedPriceComponent> components,
            OrderItem item,
            OrderService service,
            IReadOnlyList<OrderAirTransportService> coveredAirServices,
            OrderPricingReason reason,
            IIdGenerator idGenerator,
            long changeId,
            DateTimeOffset createdAt)
        {
            var segmentId = coveredAirServices.Count == 1 ? coveredAirServices[0].SegmentId : (long?)null;
            var journeyId = segmentId is { } coveredSegmentId ? SegmentJourneyId(coveredSegmentId) : (long?)null;
            var lines = new List<PricingLine>();

            foreach (var component in components)
            {
                var line = new PricingLine(
                    idGenerator.NewId(),
                    Id,
                    changeId,
                    reason,
                    PricingLineScope.OrderService,
                    component.Category,
                    component.SubCategory,
                    OrderPricingLineDirection.Credit,
                    PricingLineTreatment.CustomerPrice,
                    component.Code,
                    null,
                    null,
                    component.Amount,
                    CurrencyId,
                    component.Amount,
                    CurrencyId,
                    exchangeRateSnapshot: null,
                    refundability: null,
                    component.TaxJurisdiction,
                    component.TaxTreatment,
                    component.FeeApplicationUnit,
                    createdAt);

                line.AddAllocation(new PricingAllocation(
                    idGenerator.NewId(),
                    line.Id,
                    item.Id,
                    service.Id,
                    journeyId,
                    segmentId,
                    service.TravellerId,
                    component.Amount,
                    CurrencyId,
                    component.Amount,
                    CurrencyId));

                lines.Add(line);
            }

            return lines;
        }

        private static AcceptedPriceComponent AcceptedComponentOf(
            ResolvedAncillaryPriceLine source,
            OrderPricingLineCategory category,
            OrderPricingLineSubCategory subCategory,
            int quantity)
            => new(
                category,
                subCategory,
                source.Code,
                source.Amount * quantity,
                TaxJurisdictionSnapshot.Of(source.CountryId, source.StationAirportId),
                source.TaxTreatment,
                source.FeeApplicationUnit);

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

        private sealed record AcceptedPriceComponent(
            OrderPricingLineCategory Category,
            OrderPricingLineSubCategory SubCategory,
            string? Code,
            decimal Amount,
            TaxJurisdictionSnapshot? TaxJurisdiction,
            TaxTreatment? TaxTreatment,
            FeeApplicationUnit? FeeApplicationUnit);
    }
}
