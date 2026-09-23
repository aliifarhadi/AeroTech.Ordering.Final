using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class ExchangeRateSnapshot : ValueObject
    {
        private ExchangeRateSnapshot()
        {
        }

        public ExchangeRateSnapshot(int fromCurrencyId, int toCurrencyId, decimal rate, int decimalPlaces, string? periodId)
        {
            if (rate <= 0)
                throw ExceptionFactory.ExchangeRateMustBePositive();

            if (decimalPlaces < 0)
                throw ExceptionFactory.ExchangeRateDecimalPlacesCannotBeNegative();

            FromCurrencyId = fromCurrencyId;
            ToCurrencyId = toCurrencyId;
            Rate = rate;
            DecimalPlaces = decimalPlaces;
            PeriodId = periodId;
        }

        public int FromCurrencyId { get; private set; }

        public int ToCurrencyId { get; private set; }

        public decimal Rate { get; private set; }

        public int DecimalPlaces { get; private set; }

        public string? PeriodId { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return FromCurrencyId;
            yield return ToCurrencyId;
            yield return Rate;
            yield return DecimalPlaces;
            yield return PeriodId;
        }
    }
}
