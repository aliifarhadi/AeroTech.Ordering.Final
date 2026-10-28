using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain.Providers;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public abstract class OrderAncillaryService : OrderService
    {
        private readonly List<long> _coveredAirServiceIds = new();

        protected OrderAncillaryService()
        {
        }

        protected OrderAncillaryService(OrderAncillaryServiceArgs args, AncillaryProfile profile)
            : base(
                args.Id,
                args.OrderId,
                args.OrderItemId,
                args.TravellerId,
                args.Variant.ServiceType,
                FulfillmentProviderKeys.Ancillary,
                args.CreatedByChangeId,
                args.CreatedAt)
        {
            var offerItemId = args.Source.OfferItemId;

            if (args.Variant.Profile != profile || args.Variant.IsSeatAssignment)
                throw ExceptionFactory.AncillaryVariantDoesNotMatchProfile(offerItemId, args.Variant.Code, profile);

            if (args.Quantity <= 0)
                throw ExceptionFactory.AncillaryQuantityIsInvalid(offerItemId, args.Quantity, args.Quantity);

            if (!ScopesAgree(args.SelectionScope, args.CoverageScope) || (args.TravellerId is null) != (args.SelectionScope == SelectionScope.PerOrder))
                throw ExceptionFactory.AncillaryScopeIsInconsistent(offerItemId, args.SelectionScope, args.CoverageScope);

            if (args.CoveredAirServiceIds.Count != args.CoveredAirServiceIds.Distinct().Count() || args.CoveredAirServiceIds.Any(airServiceId => airServiceId <= 0))
                throw ExceptionFactory.AncillaryCoverageDoesNotMatchUnit(offerItemId);

            Profile = profile;
            VariantCode = args.Variant.Code;
            Quantity = args.Quantity;
            SelectionScope = args.SelectionScope;
            CoverageScope = args.CoverageScope;
            _coveredAirServiceIds.AddRange(args.CoveredAirServiceIds);
            Source = args.Source;
            Definition = args.Definition;
            Provision = args.Provision;
            Pricing = args.Pricing;
        }

        public AncillaryProfile Profile { get; private set; }

        public string VariantCode { get; private set; } = default!;

        public int Quantity { get; private set; }

        public SelectionScope SelectionScope { get; private set; }

        public ServiceCoverageScope CoverageScope { get; private set; }

        public IReadOnlyCollection<long> CoveredAirServiceIds => _coveredAirServiceIds.AsReadOnly();

        public AncillarySaleSource Source { get; private set; } = default!;

        public AncillaryDefinition Definition { get; private set; } = default!;

        public AncillaryProvision Provision { get; private set; } = default!;

        public AncillaryPricing Pricing { get; private set; } = default!;

        public static bool ScopesAgree(SelectionScope selectionScope, ServiceCoverageScope coverageScope)
            => (selectionScope, coverageScope) is
                (SelectionScope.PerOrder, ServiceCoverageScope.Order)
                or (SelectionScope.PerTraveller, ServiceCoverageScope.Journey)
                or (SelectionScope.PerTravellerPerPortion, ServiceCoverageScope.Portion)
                or (SelectionScope.PerTravellerPerFlight, ServiceCoverageScope.Sector);

        protected BusinessException MissingChoice(string choice)
            => ExceptionFactory.AncillaryChoiceIsMissing(Source.OfferItemId, VariantCode, choice);

        protected void EnsurePresent(bool isPresent, string choice)
        {
            if (!isPresent)
                throw MissingChoice(choice);
        }

        protected void EnsureValid(bool isValid, string choice)
        {
            if (!isValid)
                throw ExceptionFactory.AncillaryChoiceIsInvalid(Source.OfferItemId, VariantCode, choice);
        }

        protected static bool IsPositive(ServiceDimensions? dimensions)
            => dimensions is null or { LengthCm: > 0, WidthCm: > 0, HeightCm: > 0 };
    }
}
