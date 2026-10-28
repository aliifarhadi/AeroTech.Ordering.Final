using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class ServiceDimensions : ValueObject
    {
        private ServiceDimensions()
        {
        }

        public ServiceDimensions(
            decimal lengthCm,
            decimal widthCm,
            decimal heightCm)
        {
            LengthCm = lengthCm;
            WidthCm = widthCm;
            HeightCm = heightCm;
        }

        public decimal LengthCm { get; private set; }

        public decimal WidthCm { get; private set; }

        public decimal HeightCm { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return LengthCm;
            yield return WidthCm;
            yield return HeightCm;
        }
    }
}
