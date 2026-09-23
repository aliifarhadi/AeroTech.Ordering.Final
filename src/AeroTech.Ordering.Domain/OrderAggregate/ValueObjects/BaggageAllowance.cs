using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class BaggageAllowance : ValueObject
    {
        private BaggageAllowance()
        {
        }

        public BaggageAllowance(int pieces, decimal weight, WeightUnit unit)
        {
            if (pieces < 0)
                throw ExceptionFactory.BaggagePiecesCannotBeNegative();

            if (weight < 0)
                throw ExceptionFactory.BaggageWeightCannotBeNegative();

            Pieces = pieces;
            Weight = weight;
            Unit = unit;
        }

        public static BaggageAllowance FromSource(int pieces, decimal weight, string unit)
            => new(pieces, weight, ParseUnit(unit));

        public int Pieces { get; private set; }

        public decimal Weight { get; private set; }

        public WeightUnit Unit { get; private set; }

        private static WeightUnit ParseUnit(string unit)
            => Enum.TryParse<WeightUnit>(unit, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw ExceptionFactory.BaggageUnitIsNotRecognised(unit);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Pieces;
            yield return Weight;
            yield return Unit;
        }
    }
}
