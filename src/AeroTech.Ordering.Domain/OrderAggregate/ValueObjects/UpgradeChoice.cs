using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class UpgradeChoice : ValueObject
    {
        private UpgradeChoice()
        {
        }

        public UpgradeChoice(
            int fromCabinId,
            int toCabinId,
            UpgradeKind upgradeKind,
            bool requiresTicketExchange)
        {
            FromCabinId = fromCabinId;
            ToCabinId = toCabinId;
            UpgradeKind = upgradeKind;
            RequiresTicketExchange = requiresTicketExchange;
        }

        public int FromCabinId { get; private set; }

        public int ToCabinId { get; private set; }

        public UpgradeKind UpgradeKind { get; private set; }

        public bool RequiresTicketExchange { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return FromCabinId;
            yield return ToCabinId;
            yield return UpgradeKind;
            yield return RequiresTicketExchange;
        }
    }
}
