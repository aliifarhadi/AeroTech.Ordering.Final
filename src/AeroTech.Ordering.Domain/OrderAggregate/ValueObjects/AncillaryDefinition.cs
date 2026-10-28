using AeroTech.Framework.Core.Domain.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class AncillaryDefinition : ValueObject
    {
        private AncillaryDefinition()
        {
        }

        public AncillaryDefinition(
            long definitionVersionId,
            string serviceDefinitionRef,
            string serviceTypeCode,
            string serviceSubCode,
            string? rfic,
            string? rfisc,
            string? ssrCode)
        {
            DefinitionVersionId = definitionVersionId;
            ServiceDefinitionRef = serviceDefinitionRef;
            ServiceTypeCode = serviceTypeCode;
            ServiceSubCode = serviceSubCode;
            Rfic = rfic;
            Rfisc = rfisc;
            SsrCode = ssrCode;
        }

        public long DefinitionVersionId { get; private set; }

        public string ServiceDefinitionRef { get; private set; } = default!;

        public string ServiceTypeCode { get; private set; } = default!;

        public string ServiceSubCode { get; private set; } = default!;

        public string? Rfic { get; private set; }

        public string? Rfisc { get; private set; }

        public string? SsrCode { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return DefinitionVersionId;
            yield return ServiceDefinitionRef;
            yield return ServiceTypeCode;
            yield return ServiceSubCode;
            yield return Rfic;
            yield return Rfisc;
            yield return SsrCode;
        }
    }
}
