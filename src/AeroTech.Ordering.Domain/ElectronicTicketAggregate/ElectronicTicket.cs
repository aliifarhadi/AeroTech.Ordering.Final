using AeroTech.Framework.Core.Domain.Aggregates;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate
{
    public sealed class ElectronicTicket : AggregateRoot<long>
    {
        private readonly List<TicketCoupon> _coupons = new();
        private readonly List<TicketPriceLink> _priceLinks = new();

        private ElectronicTicket()
        {
        }

        private ElectronicTicket(
            long id,
            IssueElectronicTicketArgs args,
            DocumentAuthority authority,
            DateTimeOffset issuedAt)
        {
            Id = id;
            OriginalOrderId = args.OrderId;
            CurrentServicingOrderId = args.OrderId;
            TravellerId = args.TravellerId;
            TravellerProfileRevisionId = args.TravellerProfileRevisionId;
            IssueFulfillmentTaskId = args.IssueFulfillmentTaskId;
            DocumentNumber = args.DocumentNumber;
            IssuanceContext = args.IssuanceContext;
            Authority = authority;
            IssuedAt = issuedAt;
            CurrencyId = args.CurrencyId;
            StatusSummary = ElectronicTicketStatus.Issued;
            DocumentVersion = 1;
        }

        public long OriginalOrderId { get; private set; }

        public long CurrentServicingOrderId { get; private set; }

        public long TravellerId { get; private set; }

        public long TravellerProfileRevisionId { get; private set; }

        public long IssueFulfillmentTaskId { get; private set; }

        public string DocumentNumber { get; private set; } = default!;

        public DocumentIssuanceContext IssuanceContext { get; private set; } = default!;

        public DocumentAuthority Authority { get; private set; }

        public DateTimeOffset IssuedAt { get; private set; }

        public DateTimeOffset? VoidDeadline { get; private set; }

        public decimal IssuedTotal { get; private set; }

        public int CurrencyId { get; private set; }

        public string? ProviderReference { get; private set; }

        public ElectronicTicketStatus StatusSummary { get; private set; }

        public int DocumentVersion { get; private set; }

        public long? PredecessorElectronicTicketId { get; private set; }

        public long? PredecessorExchangeChangeId { get; private set; }

        public IReadOnlyCollection<TicketCoupon> Coupons => _coupons.AsReadOnly();

        public IReadOnlyCollection<TicketPriceLink> PriceLinks => _priceLinks.AsReadOnly();

        public static ElectronicTicket IssueLocally(
            long id,
            IssueElectronicTicketArgs args,
            IIdGenerator idGenerator,
            DateTimeOffset issuedAt)
        {
            if (string.IsNullOrWhiteSpace(args.DocumentNumber))
                throw ExceptionFactory.DocumentNumberIsRequired();

            if (args.Coupons.Count == 0)
                throw ExceptionFactory.ElectronicTicketRequiresCoupons(args.TravellerId);

            var ticket = new ElectronicTicket(id, args, DocumentAuthority.Local, issuedAt);

            foreach (var coupon in args.Coupons)
                ticket.AddCoupon(coupon, idGenerator);

            ticket.IssuedTotal = ticket._coupons.Sum(coupon => coupon.IssuanceValue);
            ticket.RaiseIssued(idGenerator);

            return ticket;
        }

        private void AddCoupon(IssueTicketCouponArgs args, IIdGenerator idGenerator)
        {
            if (args.TravellerId != TravellerId)
                throw ExceptionFactory.TicketCouponBelongsToAnotherTraveller(args.OrderServiceId, args.TravellerId);

            if (args.PriceLinks.FirstOrDefault(link => link.CurrencyId != CurrencyId) is { } foreignLink)
                throw ExceptionFactory.ElectronicTicketCurrencyIsInconsistent(foreignLink.CurrencyId, CurrencyId);

            var issuanceValue = args.PriceLinks.Sum(link => link.AttributedValue);

            if (issuanceValue < 0)
                throw ExceptionFactory.TicketCouponIssuanceValueIsNegative(args.OrderServiceId, issuanceValue);

            var coupon = new TicketCoupon(
                idGenerator.NewId(),
                Id,
                _coupons.Count + 1,
                args.OrderServiceId,
                args.OrderSegmentId,
                args.OrderFareComponentId,
                args.IssuedSegment,
                args.FareBasis,
                args.BookingClass,
                args.RbdId,
                args.CabinClassId,
                args.BaggageAllowance,
                issuanceValue,
                CurrencyId);

            _coupons.Add(coupon);

            foreach (var link in args.PriceLinks)
                _priceLinks.Add(new TicketPriceLink(
                    idGenerator.NewId(),
                    Id,
                    coupon.Id,
                    link.PricingLineId,
                    link.PricingAllocationId,
                    link.AttributedValue,
                    link.CurrencyId));
        }

        private void RaiseIssued(IIdGenerator idGenerator)
            => Causes(new ElectronicTicketIssued(
                idGenerator.NewId().ToString(),
                Id.ToString(),
                IssuedAt,
                Id,
                OriginalOrderId,
                TravellerId,
                IssueFulfillmentTaskId,
                DocumentNumber,
                IssuanceContext.IssuerCarrierId,
                IssuanceContext.IssuingOfficeId,
                Authority,
                CurrencyId,
                IssuedTotal,
                DocumentVersion,
                PredecessorElectronicTicketId,
                _coupons
                    .Select(coupon => new IssuedTicketCoupon(
                        coupon.Id,
                        coupon.CouponNumber,
                        coupon.CurrentOrderServiceId,
                        coupon.OrderSegmentId,
                        coupon.IssuanceValue,
                        coupon.PredecessorTicketCouponId))
                    .ToList()));
    }
}
