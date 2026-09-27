using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Shared.Enums;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects
{
    public sealed class DocumentIssuanceContext : ValueObject
    {
        private DocumentIssuanceContext()
        {
        }

        public DocumentIssuanceContext(
            int issuerCarrierId,
            int? validatingCarrierId,
            long? issuingOfficeId,
            long? issuedByActorId,
            long? travelAgencyId,
            string? agencyIataNumber,
            string? pcc,
            SalesChannel? salesChannel,
            string? sourceFormOfPaymentCode)
        {
            IssuerCarrierId = issuerCarrierId;
            ValidatingCarrierId = validatingCarrierId;
            IssuingOfficeId = issuingOfficeId;
            IssuedByActorId = issuedByActorId;
            TravelAgencyId = travelAgencyId;
            AgencyIataNumber = agencyIataNumber;
            Pcc = pcc;
            SalesChannel = salesChannel;
            SourceFormOfPaymentCode = sourceFormOfPaymentCode;
        }

        public int IssuerCarrierId { get; private set; }

        public int? ValidatingCarrierId { get; private set; }

        public long? IssuingOfficeId { get; private set; }

        public long? IssuedByActorId { get; private set; }

        public long? TravelAgencyId { get; private set; }

        public string? AgencyIataNumber { get; private set; }

        public string? Pcc { get; private set; }

        public SalesChannel? SalesChannel { get; private set; }

        public string? SourceFormOfPaymentCode { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return IssuerCarrierId;
            yield return ValidatingCarrierId;
            yield return IssuingOfficeId;
            yield return IssuedByActorId;
            yield return TravelAgencyId;
            yield return AgencyIataNumber;
            yield return Pcc;
            yield return SalesChannel;
            yield return SourceFormOfPaymentCode;
        }
    }
}
