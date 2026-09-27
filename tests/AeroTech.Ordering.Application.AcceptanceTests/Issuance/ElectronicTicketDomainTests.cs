using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.TicketFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class ElectronicTicketDomainTests
{
    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Ticket_requires_at_least_one_coupon()
    {
        var exception = Assert.Throws<BusinessException>(() => Issue(Args([])));

        Assert.Equal(2795, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Ticket_requires_a_document_number(string documentNumber)
    {
        var exception = Assert.Throws<BusinessException>(() => Issue(Args([Coupon(101)]) with { DocumentNumber = documentNumber }));

        Assert.Equal(2798, exception.Code);
    }

    [Fact]
    public void Coupons_are_numbered_uniquely_in_the_planned_order()
    {
        var ticket = Issue(Args([Coupon(303), Coupon(101), Coupon(202)]));

        Assert.Equal(
            [(1, 303L), (2, 101L), (3, 202L)],
            ticket.Coupons.Select(coupon => (coupon.CouponNumber, coupon.OriginalOrderServiceId)));
    }

    [Fact]
    public void Issued_total_reconciles_to_coupon_and_price_link_values()
    {
        var ticket = Issue(Args([Coupon(101, 100m, 20m), Coupon(202, 80m, -5m)]));

        Assert.Equal([120m, 75m], ticket.Coupons.Select(coupon => coupon.IssuanceValue));
        Assert.Equal(195m, ticket.IssuedTotal);
        Assert.Equal(ticket.IssuedTotal, ticket.PriceLinks.Sum(link => link.AttributedValue));
        Assert.All(ticket.PriceLinks, link => Assert.Contains(ticket.Coupons, coupon => coupon.Id == link.TicketCouponId));
    }

    [Fact]
    public void Ticket_starts_issued_at_version_one_under_local_authority()
    {
        var ticket = Issue(Args([Coupon(101)]));

        Assert.Equal(
            (ElectronicTicketStatus.Issued, 1, DocumentAuthority.Local, 5L, 5L, 9L),
            (ticket.StatusSummary, ticket.DocumentVersion, ticket.Authority, ticket.OriginalOrderId, ticket.CurrentServicingOrderId, ticket.IssueFulfillmentTaskId));
        Assert.Null(ticket.VoidDeadline);
        Assert.Null(ticket.ProviderReference);
        Assert.Null(ticket.PredecessorElectronicTicketId);
        Assert.Null(ticket.PredecessorExchangeChangeId);
    }

    [Fact]
    public void Issued_segment_and_context_are_immutable_value_snapshots()
    {
        var segment = Segment();
        var ticket = Issue(Args([Coupon(101) with { IssuedSegment = segment }]));

        Assert.Equal(segment, ticket.Coupons.Single().IssuedSegment);
        Assert.All(
            typeof(IssuedSegmentSnapshot).GetProperties().Concat(typeof(DocumentIssuanceContext).GetProperties()),
            property => Assert.False(property.SetMethod?.IsPublic ?? false, property.Name));
    }

    [Fact]
    public void Coupons_start_financially_open_under_local_control()
    {
        var coupon = Issue(Args([Coupon(101)])).Coupons.Single();

        Assert.Equal((TicketCouponFinancialStatus.Open, TicketCouponControlStatus.Local), (coupon.FinancialStatus, coupon.ControlStatus));
        Assert.Equal(coupon.OriginalOrderServiceId, coupon.CurrentOrderServiceId);
        Assert.Null(coupon.PredecessorTicketCouponId);
        Assert.Null(coupon.ProviderCouponStatusCode);
        Assert.Null(coupon.NotValidBefore);
        Assert.Null(coupon.NotValidAfter);
        Assert.Null(coupon.UsedAt);
        Assert.Null(coupon.UsageReference);
    }

    [Fact]
    public void Coupon_of_another_traveller_is_rejected()
    {
        var exception = Assert.Throws<BusinessException>(() => Issue(Args([Coupon(101) with { TravellerId = TravellerId + 1 }])));

        Assert.Equal(2796, exception.Code);
    }

    [Fact]
    public void Price_link_in_another_currency_is_rejected()
    {
        var coupon = Coupon(101) with { PriceLinks = [new IssueTicketPriceLinkArgs(1, 2, 100m, CurrencyId + 1)] };

        var exception = Assert.Throws<BusinessException>(() => Issue(Args([coupon])));

        Assert.Equal(2797, exception.Code);
    }

    [Fact]
    public void Negative_coupon_value_is_rejected_without_clamping()
    {
        var exception = Assert.Throws<BusinessException>(() => Issue(Args([Coupon(101, 10m, -20m)])));

        Assert.Equal(2784, exception.Code);
    }

    [Fact]
    public void Issued_ticket_raises_the_issued_event_with_its_coupons()
    {
        var ticket = Issue(Args([Coupon(101), Coupon(202)]));

        var issued = Assert.IsType<ElectronicTicketIssued>(Assert.Single(ticket.GetEvents()));
        Assert.Equal((ticket.Id, 9L, ticket.DocumentNumber, ticket.IssuedTotal), (issued.ElectronicTicketId, issued.IssueFulfillmentTaskId, issued.DocumentNumber, issued.IssuedTotal));
        Assert.Equal(ticket.Coupons.Select(coupon => coupon.Id), issued.Coupons.Select(coupon => coupon.TicketCouponId));
    }

    private ElectronicTicket Issue(IssueElectronicTicketArgs args) => TicketFixture.Issue(_ids, args);
}
