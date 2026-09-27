using System.Globalization;
using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.DocumentStockAggregate
{
    public sealed class DocumentStock : AggregateRoot<long>
    {
        public const string NoCheckDigitProfile = "None";

        private readonly List<DocumentStockAllocation> _allocations = new();

        private DocumentStock()
        {
        }

        private DocumentStock(
            long id,
            int ownerAirlineId,
            long? officeId,
            AccountableDocumentKind documentKind,
            string prefix,
            int serialWidth,
            string checkDigitProfile,
            long rangeFrom,
            long rangeTo)
        {
            Id = id;
            OwnerAirlineId = ownerAirlineId;
            OfficeId = officeId;
            DocumentKind = documentKind;
            Prefix = prefix;
            SerialWidth = serialWidth;
            CheckDigitProfile = checkDigitProfile;
            RangeFrom = rangeFrom;
            RangeTo = rangeTo;
            NextNumber = rangeFrom;
            Status = DocumentStockStatus.Active;
        }

        public int OwnerAirlineId { get; private set; }

        public long? OfficeId { get; private set; }

        public AccountableDocumentKind DocumentKind { get; private set; }

        public string Prefix { get; private set; } = default!;

        public int SerialWidth { get; private set; }

        public string CheckDigitProfile { get; private set; } = default!;

        public long RangeFrom { get; private set; }

        public long RangeTo { get; private set; }

        public long NextNumber { get; private set; }

        public DocumentStockStatus Status { get; private set; }

        public IReadOnlyCollection<DocumentStockAllocation> Allocations => _allocations.AsReadOnly();

        public long RemainingNumbers => RangeTo - NextNumber + 1;

        public static DocumentStock Define(
            long id,
            int ownerAirlineId,
            long? officeId,
            AccountableDocumentKind documentKind,
            string prefix,
            int serialWidth,
            string checkDigitProfile,
            long rangeFrom,
            long rangeTo)
        {
            if (checkDigitProfile != NoCheckDigitProfile)
                throw ExceptionFactory.DocumentStockCheckDigitProfileIsUnsupported(checkDigitProfile);

            if (string.IsNullOrWhiteSpace(prefix)
                || serialWidth <= 0
                || rangeFrom <= 0
                || rangeTo < rangeFrom
                || DigitsOf(rangeTo) > serialWidth)
                throw ExceptionFactory.DocumentStockRangeIsInvalid(rangeFrom, rangeTo, serialWidth, prefix);

            return new DocumentStock(id, ownerAirlineId, officeId, documentKind, prefix, serialWidth, checkDigitProfile, rangeFrom, rangeTo);
        }

        public bool Overlaps(long rangeFrom, long rangeTo) => RangeFrom <= rangeTo && rangeFrom <= RangeTo;

        public void EnsureCanIssue(AccountableDocumentKind documentKind, int documentCount)
        {
            if (DocumentKind != documentKind)
                throw ExceptionFactory.DocumentStockKindMismatch(Id, DocumentKind, documentKind);

            if (CheckDigitProfile != NoCheckDigitProfile)
                throw ExceptionFactory.DocumentStockCheckDigitProfileIsUnsupported(CheckDigitProfile);

            if (Status == DocumentStockStatus.Exhausted)
                throw ExceptionFactory.DocumentStockIsExhausted(Id);

            if (Status != DocumentStockStatus.Active)
                throw ExceptionFactory.DocumentStockIsNotActive(Id, Status);

            if (RemainingNumbers < documentCount)
                throw ExceptionFactory.DocumentStockHasInsufficientNumbers(Id, RemainingNumbers, documentCount);
        }

        public DocumentStockAllocation Allocate(
            long issueFulfillmentTaskId,
            string documentRole,
            IIdGenerator idGenerator,
            DateTimeOffset allocatedAt)
        {
            var existing = _allocations.FirstOrDefault(allocation => allocation.IssueFulfillmentTaskId == issueFulfillmentTaskId
                                                                     && allocation.DocumentRole == documentRole
                                                                     && allocation.State != StockNumberState.Retired);

            if (existing is not null)
                return existing;

            EnsureCanIssue(DocumentKind, 1);

            var allocation = new DocumentStockAllocation(
                idGenerator.NewId(),
                Id,
                issueFulfillmentTaskId,
                documentRole,
                NextNumber,
                DocumentNumberOf(NextNumber),
                allocatedAt);

            _allocations.Add(allocation);
            NextNumber++;

            if (NextNumber > RangeTo)
                Status = DocumentStockStatus.Exhausted;

            return allocation;
        }

        public void MarkIssued(long allocationId, DateTimeOffset settledAt)
            => AllocationOf(allocationId).Settle(StockNumberState.Issued, settledAt);

        public void Retire(long allocationId, DateTimeOffset settledAt)
            => AllocationOf(allocationId).Settle(StockNumberState.Retired, settledAt);

        private DocumentStockAllocation AllocationOf(long allocationId)
            => _allocations.Single(allocation => allocation.Id == allocationId);

        private string DocumentNumberOf(long serial)
            => Prefix + serial.ToString(CultureInfo.InvariantCulture).PadLeft(SerialWidth, '0');

        private static int DigitsOf(long value) => value.ToString(CultureInfo.InvariantCulture).Length;
    }
}
