using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderUpgradeService : OrderAncillaryService
    {
        private OrderUpgradeService()
        {
        }

        public OrderUpgradeService(
            OrderAncillaryServiceArgs args,
            int fromCabinId,
            int toCabinId,
            UpgradeKind upgradeKind,
            bool requiresTicketExchange)
            : base(args, AncillaryProfile.Upgrade)
        {
            if (upgradeKind != UpgradeKind.FixedAncillary)
                throw ExceptionFactory.AncillaryUpgradeIsNotFixedPriced(Source.OfferItemId, upgradeKind);

            EnsureValid(fromCabinId > 0, nameof(FromCabinId));
            EnsureValid(toCabinId > 0, nameof(ToCabinId));

            FromCabinId = fromCabinId;
            ToCabinId = toCabinId;
            UpgradeKind = upgradeKind;
            RequiresTicketExchange = requiresTicketExchange;
        }

        public int FromCabinId { get; private set; }

        public int ToCabinId { get; private set; }

        public UpgradeKind UpgradeKind { get; private set; }

        public bool RequiresTicketExchange { get; private set; }
    }
}
